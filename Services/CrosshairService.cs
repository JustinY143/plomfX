using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace plomfX.Services
{
    public class CrosshairCategory
    {
        public string Name { get; set; } = string.Empty;
        public List<string> ImagePaths { get; set; } = new();
    }

    public static class CrosshairService
    {
        // Go up from bin/Debug/netX.X-windows to project root
        private static readonly string CrosshairsFolder = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crosshairs");
        public static List<CrosshairCategory> GetCategories()
        {
            var categories = new List<CrosshairCategory>();

            if (!Directory.Exists(CrosshairsFolder))
                return categories;

            foreach (var dir in Directory.GetDirectories(CrosshairsFolder))
            {
                var categoryName = Path.GetFileName(dir);
                var pngFiles = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly).ToList();
                
                // Skip if no PNGs
                if (pngFiles.Count == 0)
                    continue;

                categories.Add(new CrosshairCategory
                {
                    Name = categoryName,
                    ImagePaths = pngFiles
                });
            }

            return categories.OrderBy(c => c.Name).ToList();
        }
    }
}