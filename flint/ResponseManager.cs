using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using flint.Responses;

namespace flint
{
    internal class ResponseManager
    {
        private static readonly Dictionary<Endpoint, List<ResponseMatch>> _endpointToResponseMap;

        static ResponseManager()
        {
            _endpointToResponseMap = new Dictionary<Endpoint, List<ResponseMatch>>();

            var assembly = typeof( IResponse ).Assembly;
            foreach ( var responseType in assembly.GetTypes().Where( x => x.GetInterfaces().Contains( typeof( IResponse ) ) ) )
            {
                var endpointAttribute = responseType.GetCustomAttributes( typeof( EndpointAttribute ), false )
                                .OfType<EndpointAttribute>()
                                .FirstOrDefault();
                if ( endpointAttribute != null )
                {
                    List<ResponseMatch> responseMatches;
                    if ( _endpointToResponseMap.TryGetValue( endpointAttribute.Endpoint, out responseMatches ) == false )
                        _endpointToResponseMap.Add( endpointAttribute.Endpoint, responseMatches = new List<ResponseMatch>() );

                    //TODO: What happens if there is not a default constructor? multiple constructors?
                    Expression body = Expression.New( responseType );
                    var func = (Func<IResponse>)Expression.Lambda( body ).Compile();

                    responseMatches.Add( new ResponseMatch( func, endpointAttribute.GetPredicate() ) );
                }
            }
        }


        private IResponseSetter _pendingTransaction;

        public IResponseTransaction<T> GetTransaction<T>() where T : class, IResponse, new()
        {
            var rv = new ResponseTransaction<T>( this );
            _pendingTransaction = rv;
            return rv;
        }

        public IResponse HandleResponse( Endpoint endpoint, byte[] payload )
        {
            //TODO: might be better to create a logs response...
            if ( endpoint == Endpoint.Logs && _pendingTransaction != null )
            {
                _pendingTransaction.SetError(payload);
                return null;
            }

            List<ResponseMatch> responseMatches;
            if ( _endpointToResponseMap.TryGetValue( endpoint, out responseMatches ) )
            {
                IResponse rv = responseMatches.Select( x => x.GetResponse( payload ) ).FirstOrDefault( x => x != null );
                if ( rv != null && _pendingTransaction != null )
                    _pendingTransaction.SetPayload( payload );
                //TODO: Consider creating a generic response
#if DEBUG
                //Using this to find responses that do now have classes to handle them
                if ( rv == null && System.Diagnostics.Debugger.IsAttached )
                    System.Diagnostics.Debugger.Break();
#endif
            }
            return null;
        }

        private class ResponseMatch
        {
            private readonly Func<IResponse> _responseFactory;
            private readonly Func<byte[], bool> _condition;

            public ResponseMatch( Func<IResponse> responseFactory, Func<byte[], bool> condition )
            {
                if ( responseFactory == null ) throw new ArgumentNullException( "responseFactory" );
                _responseFactory = responseFactory;
                _condition = condition;
            }

            public IResponse GetResponse( byte[] payload )
            {
                if ( _condition == null || _condition( payload ?? new byte[0] ) )
                {
                    return _responseFactory();
                }
                return null;
            }
        }
        
        private interface IResponseSetter
        {
            void SetPayload( byte[] payload );
            void SetError( byte[] errorPayload );
        }

        private class ResponseTransaction<T> : IResponseSetter, IResponseTransaction<T> where T : class, IResponse, new()
        {
            private readonly T _response;
            private readonly ResponseManager _manager;
            private readonly ManualResetEvent _resetEvent;

            public ResponseTransaction( ResponseManager manager )
            {
                if ( manager == null ) throw new ArgumentNullException( "manager" );
                _manager = manager;
                _resetEvent = new ManualResetEvent( false );
                _response = new T();
            }
            
            public void Dispose()
            {
                if ( ReferenceEquals( _manager._pendingTransaction, this ) )
                    _manager._pendingTransaction = null;
            }

            public T AwaitResponse( TimeSpan timeout )
            {
                if (_resetEvent.WaitOne(timeout))
                    _response.SetError( "Timed out waiting for a response" );
                return _response;
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
        }
    }

    internal interface IResponseTransaction<out T> : IDisposable where T : class, IResponse
    {
        T AwaitResponse( TimeSpan timeout );
    }
}