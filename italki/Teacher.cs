namespace italki
{
    public class Teacher
    {
        public int UserId { get; set; }
        public string Nickname { get; set; }
        public string OriginCountryId { get; set; }
        public int SessionCount { get; set; }
        public int StudentCount { get; set; }
        public double Rating => (double)SessionCount / StudentCount;
        public string Url => $"https://www.italki.com/teacher/{UserId}";
        public int MinPrice { get; set; }
    }
}
