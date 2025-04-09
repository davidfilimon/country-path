namespace orase.Models
{
    public class SearchResult
    {
        public List<string> Route { get; set; }
        public int TotalDistance { get; set; }
        public long ExecutionTime { get; set; }
        public int NodesVisited { get; set; }
    }

}
