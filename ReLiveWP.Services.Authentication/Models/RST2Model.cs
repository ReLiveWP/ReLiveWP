namespace ReLiveWP.Services.Authentication.Models
{
    public class RST2Model
    {
        public string TimeZ { get; set; }
        public string TomorrowZ { get; set; }
        public string Time5MZ { get; set; }
        public string PUIDHex { get; set; }
        public string CID { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string IP { get; set; }

        public string Token { get; set; }
        public RST2Token[] Tokens { get; set; }
    }

    public class RST2Token
    {
        public string Domain { get; set; }
        public string Token { get; set; }
        public string BinarySecret { get; set; }
    }
}
