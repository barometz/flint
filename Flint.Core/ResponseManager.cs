using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Flint.Core.Responses;

namespace Flint.Core
{
    internal class ResponseManager
    {
        private static readonly Dictionary<Endpoint, List<ResponseMatch>> _endpointToResponseMap;

        private readonly object _pendingTransactionSyncLock = new object();
        private IResponseSetter _pendingTransaction;

        static ResponseManager()
        {
            _endpointToResponseMap = new Dictionary<Endpoint, List<ResponseMatch>>();

            Assembly assembly = typeof (IResponse).GetTypeInfo().Assembly;
            foreach (Type responseType in assembly.ExportedTypes.Where(x => x.GetTypeInfo().ImplementedInterfaces.Contains(typeof (IResponse))))
            {
                EndpointAttribute endpointAttribute =
                    responseType.GetTypeInfo().GetCustomAttributes(typeof (EndpointAttribute), false)
                                .OfType<EndpointAttribute>()
                                .FirstOrDefault();
                if (endpointAttribute != null)
                {
                    List<ResponseMatch> responseMatches;
                    if (_endpointToResponseMap.TryGetValue(endpointAttribute.Endpoint, out responseMatches) == false)
                        _endpointToResponseMap.Add(endpointAttribute.Endpoint,
                                                   responseMatches = new List<ResponseMatch>());

                    //TODO: What happens if there is not a default constructor? multiple constructors?
                    Expression body = Expression.New(responseType);
                    var func = (Func<IResponse>) Expression.Lambda(body).Compile();

                    responseMatches.Add(new ResponseMatch(func, endpointAttribute.GetPredicate()));
                }
            }
        }

        public IResponseTransaction<T> GetTransaction<T>() where T : class, IResponse, new()
        {
            lock (_pendingTransactionSyncLock)
            {
                if (_pendingTransaction != null)
                    throw new InvalidOperationException();
                var rv = new ResponseTransaction<T>(this);
                _pendingTransaction = rv;
                return rv;
            }
        }

        public IResponse HandleResponse( Endpoint endpoint, byte[] payload )
        {
            lock (_pendingTransactionSyncLock)
            {
                //TODO: might be better to create a logs response type...
                if (endpoint == Endpoint.Logs && _pendingTransaction != null)
                {
                    _pendingTransaction.SetError(payload);
                    return null;
                }

                IResponse rv = null;
                List<ResponseMatch> responseMatches;
                if (_endpointToResponseMap.TryGetValue(endpoint, out responseMatches))
                {
                    rv = responseMatches.Select(x => x.GetResponse(payload)).FirstOrDefault(x => x != null);
                    if (rv != null)
                    {
                        if (_pendingTransaction != null && rv.GetType() == _pendingTransaction.ResponseType)
                            _pendingTransaction.SetPayload(payload);
                        rv.SetPayload(payload);
                    }

                    //TODO: generic response or event for unhandled responses
#if DEBUG
                    //Using this to find responses that do now have classes to handle them
                    if (rv == null && Debugger.IsAttached)
                        Debugger.Break();
#endif
                }
                return rv;
            }
        }

        private interface IResponseSetter
        {
            Type ResponseType { get; }
            void SetPayload( byte[] payload );
            void SetError( byte[] errorPayload );
        }

        private class ResponseMatch
        {
            private readonly Func<byte[], bool> _condition;
            private readonly Func<IResponse> _responseFactory;

            public ResponseMatch( Func<IResponse> responseFactory, Func<byte[], bool> condition )
            {
                if (responseFactory == null) throw new ArgumentNullException("responseFactory");
                _responseFactory = responseFactory;
                _condition = condition;
            }

            public IResponse GetResponse( byte[] payload )
            {
                if (_condition == null || _condition(payload ?? new byte[0]))
                {
                    return _responseFactory();
                }
                return null;
            }
        }

        private class ResponseTransaction<T> : IResponseSetter, IResponseTransaction<T>
            where T : class, IResponse, new()
        {
            private readonly ResponseManager _manager;
            private readonly ManualResetEvent _resetEvent;
            private readonly T _response;

            public ResponseTransaction( ResponseManager manager )
            {
                if (manager == null) throw new ArgumentNullException("manager");
                _manager = manager;
                _resetEvent = new ManualResetEvent(false);
                _response = new T();
            }

            public Type ResponseType
            {
                get { return typeof (T); }
            }

            void IResponseSetter.SetPayload( byte[] payload )
            {
                _response.SetPayload(payload);
                _resetEvent.Set();
            }

            void IResponseSetter.SetError( byte[] errorPayload )
            {
                _response.SetError(errorPayload);
                _resetEvent.Set();
            }

            public void Dispose()
            {
                lock (_manager._pendingTransactionSyncLock)
                {
                    if (ReferenceEquals(_manager._pendingTransaction, this))
                        _manager._pendingTransaction = null;
                }
            }

            public T AwaitResponse( TimeSpan timeout )
            {
                if (_resetEvent.WaitOne(timeout) == false)
                    _response.SetError("Timed out waiting for a response");
                return _response;
            }
        }
    }

    internal interface IResponseTransaction<out T> : IDisposable where T : class, IResponse
    {
        T AwaitResponse( TimeSpan timeout );
    }
}