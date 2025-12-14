namespace BasicLibrary
{
    public interface IParameter
    {
        public enum SourceType : int
        {
            None = 0,
            Command = 1,
            Registry = 2,
            App = 3
        }

        public string Name { get; }
        public object Value { get; set; }
        public SourceType Source { get; set; }
    }
}
