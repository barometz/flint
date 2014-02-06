using System.Diagnostics;

namespace flint.Responses
{
    public class AppbankResponse : ResponseBase
    {
        public AppBank AppBank { get; private set; }

        public string ST { get; set; }

        public AppbankResponse()
        {
            ST = new StackTrace(true).ToString();
        }

        protected override void Load( byte[] payload )
        {
            AppBank = new AppBank(payload);
        }
    }
}