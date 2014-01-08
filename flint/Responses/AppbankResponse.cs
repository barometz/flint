namespace flint.Responses
{
    public class AppbankResponse : ResponseBase
    {
        public AppBank AppBank { get; private set; }

        public override void Load( byte[] payload )
        {
            AppBank = new AppBank(payload);
        }
    }
}