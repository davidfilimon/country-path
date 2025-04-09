namespace orase.Models
{
    public class Route
    {
        public Country Target { get; set; }

        public int Distance { get; set; }

        public Route(Country target, int distance)
        {
            Target = target;
            Distance = distance;
        }
    }
}
