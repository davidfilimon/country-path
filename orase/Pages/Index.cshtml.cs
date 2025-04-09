using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using orase.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System;

namespace orase.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private Dictionary<string, Country> _countries = new();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            InitializeCountries();
        }

        private void InitializeCountries()
        {
            
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/data/rute.json");
            string jsonData = System.IO.File.ReadAllText(jsonPath);         
            var container = JsonSerializer.Deserialize<ConnectionsContainer>(jsonData);

            foreach (var connection in container.connections)
            {
                if (!_countries.ContainsKey(connection.from))
                    _countries[connection.from] = new Country(connection.from);
                if (!_countries.ContainsKey(connection.to))
                    _countries[connection.to] = new Country(connection.to);

                
                _countries[connection.from].AdjacentCountries.Add(new Models.Route(_countries[connection.to], connection.distance));
                _countries[connection.to].AdjacentCountries.Add(new Models.Route(_countries[connection.from], connection.distance));
            }
        }

        
        public JsonResult OnGetRoute(string start, string end, string method)
        {
            if (!_countries.ContainsKey(start) || !_countries.ContainsKey(end))
                return new JsonResult(new { error = "Tara invalida!" });

            SearchResult result = method == "a*" ? AStarSearch(start, end) : BFS(start, end);

            return new JsonResult(result);
        }

        private SearchResult BFS(string start, string end)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int nodesVisited = 0;

            Queue<(List<string> path, int distance)> queue = new();
            queue.Enqueue((new List<string> { start }, 0));
            HashSet<string> visited = new();

            while (queue.Count > 0)
            {
                var (path, distance) = queue.Dequeue();
                nodesVisited++;
                var lastCountry = path.Last();

                if (lastCountry == end)
                {
                    sw.Stop();
                    return new SearchResult { Route = path, TotalDistance = distance, ExecutionTime = sw.ElapsedTicks * 1000 / System.Diagnostics.Stopwatch.Frequency, NodesVisited = nodesVisited };
                }

                if (!visited.Contains(lastCountry))
                {
                    visited.Add(lastCountry);
                    foreach (var route in _countries[lastCountry].AdjacentCountries)
                    {
                        string nextCountry = route.Target.Name;
                        if (!visited.Contains(nextCountry))
                        {
                            var newPath = new List<string>(path) { nextCountry };
                            queue.Enqueue((newPath, distance + route.Distance));
                        }
                    }
                }
            }

            sw.Stop();
            return new SearchResult { Route = new List<string> { "Nu exista ruta!" }, TotalDistance = 0, ExecutionTime = sw.ElapsedTicks * 1000 / System.Diagnostics.Stopwatch.Frequency, NodesVisited = nodesVisited };
        }


        private SearchResult AStarSearch(string start, string end)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int nodesVisited = 0;

            var heuristics = ComputeHeuristics(end);
            var openSet = new List<(string country, List<string> path, int g, int f)>();
            openSet.Add((start, new List<string> { start }, 0, Heuristic(start, heuristics)));

            HashSet<string> visited = new();

            while (openSet.Count > 0)
            {
                openSet = openSet.OrderBy(n => n.f).ToList();
                var (country, path, g, f) = openSet[0];
                openSet.RemoveAt(0);
                nodesVisited++;

                if (country == end)
                {
                    sw.Stop();
                    return new SearchResult { Route = path, TotalDistance = g, ExecutionTime = sw.ElapsedTicks * 1000 / System.Diagnostics.Stopwatch.Frequency, NodesVisited = nodesVisited };
                }

                if (!visited.Contains(country))
                {
                    visited.Add(country);
                    foreach (var route in _countries[country].AdjacentCountries)
                    {
                        string nextCountry = route.Target.Name;
                        if (!visited.Contains(nextCountry))
                        {
                            var newPath = new List<string>(path) { nextCountry };
                            int gNew = g + route.Distance;
                            int fNew = gNew + Heuristic(nextCountry, heuristics);
                            openSet.Add((nextCountry, newPath, gNew, fNew));
                        }
                    }
                }
            }

            sw.Stop();
            return new SearchResult { Route = new List<string> { "Nu exista ruta!" }, TotalDistance = 0, ExecutionTime = sw.ElapsedTicks * 1000 / System.Diagnostics.Stopwatch.Frequency, NodesVisited = nodesVisited };
        }


        private int Heuristic(string country, Dictionary<string, int> heuristics)
        {
            return heuristics.ContainsKey(country) ? heuristics[country] : 0;
        }

        
        private Dictionary<string, int> ComputeHeuristics(string target)
        {
            var distances = new Dictionary<string, int>();
            foreach (var key in _countries.Keys)
                distances[key] = int.MaxValue;
            distances[target] = 0;

            var queue = new List<(string country, int distance)>();
            queue.Add((target, 0));

            while (queue.Any())
            {
                var current = queue.OrderBy(x => x.distance).First();
                queue.Remove(current);

                foreach (var route in _countries[current.country].AdjacentCountries)
                {
                    int alt = current.distance + route.Distance;
                    if (alt < distances[route.Target.Name])
                    {
                        distances[route.Target.Name] = alt;
                        queue.Add((route.Target.Name, alt));
                    }
                }
            }
            return distances;
        }

        public JsonResult OnGetCountries()
        {
            return new JsonResult(_countries.Keys.ToList());
        }
    }   
    
}
