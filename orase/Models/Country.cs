namespace orase.Models
{
    public class Country
    {
        public string Name { get; set; }
        public List<Route> AdjacentCountries { get; set; } = new List<Route>();

        public Country(string name)
        {
            Name = name;
        }

    }
}
