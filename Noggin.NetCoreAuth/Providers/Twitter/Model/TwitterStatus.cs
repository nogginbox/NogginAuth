namespace Noggin.NetCoreAuth.Providers.Twitter.Model
{
    public class TwitterStatus
    {
        public object Contributors { get; set; }
        public Geo Coordinates { get; set; }
        public string CreatedAt { get; set; }
        public bool Favorited { get; set; }
        public Geo Geo { get; set; }
        public long Id { get; set; }
        public string Idtr { get; set; }
        public string InReplyToScreenName { get; set; }
        public long InReplyToStatusId { get; set; }
        public string InReplyToStatusIdStr { get; set; }
        public long InReplyToUserId { get; set; }
        public string InReplyToUserIdStr { get; set; }
        public Place Place { get; set; }
        public long RetweetCount { get; set; }
        public bool Retweeted { get; set; }
        public string Source { get; set; }
        public string Text { get; set; }
        public bool Truncated { get; set; }
    }
}
