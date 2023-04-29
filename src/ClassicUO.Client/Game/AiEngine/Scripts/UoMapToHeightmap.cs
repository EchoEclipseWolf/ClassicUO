using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Assets;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static ClassicUO.Game.UltimaLive;
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class UoMapToHeightmap : BaseAITask
    {
        private static double[] X = { -5, 0, 16, 30, 48 };
        private static double[] Y = { 32662, 32767, 33110, 33411, 33795 };
        private static ConcurrentDictionary<short, ushort> _cachedHeights = new();

        public UoMapToHeightmap() : base("UO Map To Heightmap")
        {
        }

        public override async Task<bool> Pulse() {

            int min = -255;
            int max = 255;

            Parallel.For(min, max + 1, i => {
                int tryCount = 0;
                while (!_cachedHeights.TryAdd((short) i, CalculateNewIntensityFromHeight((int) i))) {
                    ++tryCount;
                }
            });

            var orderedDict = _cachedHeights.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            var strings = orderedDict.Select(pair => $"{pair.Key},{pair.Value}").ToList();
            File.WriteAllLines("C:\\EchoClassicUO\\HeightMapDictionary.csv", strings);

            CreateGreyscaleBitmap(9072, 9072);
            AiCore.Instance.StopScript();
            GameActions.MessageOverhead($"[UoMapToHeightmap] Finished", Player.Serial);

            return true;
        }

        public void CreateGreyscaleBitmap(int width, int height)
        {
            using (var image = new Image<Rgba64>(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort intensity = SetPixel(x, y);
                        image[x, y] = new Rgba64(intensity, intensity, intensity, intensity);
                    }
                }

                image.Save("C:\\testoutput\\greyscale_bitmap.png");
            }
        }

        private static bool IsWater(GameObject go) {
            if (go == null) {
                return false;
            }

            var graphic = go.Graphic;

            if (go is Land) {
                if (graphic is >= 0xA8 and <= 0xAB) {
                    return true;
                }

                if (graphic is >= 0x132 and <= 0x137) {
                    return true;
                }

                if (graphic is >= 0x3FF0 and <= 0x3FF3) {
                    return true;
                }
            }

            if (go is Static) {
                if (graphic is >= 6039 and <= 6066) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlackOrStars(GameObject go) {
            if (go == null) {
                return false;
            }

            var graphic = go.Graphic;

            if (go is Land) {
                if (graphic == 0x244) {
                    return true;
                }

                if (graphic is >= 0x1F8 and <= 0x1FF) {
                    return true;
                }

                if (graphic is >= 0x219 and <= 0x21B) {
                    return true;
                }
            }

            if (go is Static) {
                
            }

            return false;
        }

        public static ushort CalculateNewIntensityFromHeight(int x) {
            if (x < -255 || x > 255)
            {
                throw new ArgumentOutOfRangeException("Input x must be between -255 and 255");
            }

            if (_cachedHeights.TryGetValue((short) x, out var cachedHeight)) {
                return cachedHeight;
            }


            if (x < X[0])
            {
                double slope = (Y[1] - Y[0]) / (X[1] - X[0]);
                return (ushort)Math.Round(Y[0] + slope * (x - X[0]));
            }
            else if (x > X[X.Length - 1])
            {
                double slope = (Y[Y.Length - 1] - Y[Y.Length - 2]) / (X[X.Length - 1] - X[X.Length - 2]);
                return (ushort)Math.Round(Y[Y.Length - 1] + slope * (x - X[X.Length - 1]));
            }

            double[] A = new double[X.Length];
            double[] B = new double[X.Length];
            double[] D = new double[X.Length];

            double[] h = new double[X.Length - 1];
            double[] alpha = new double[X.Length - 1];

            for (int i = 0; i < X.Length - 1; i++)
            {
                h[i] = X[i + 1] - X[i];
            }

            for (int i = 1; i < X.Length - 1; i++)
            {
                alpha[i] = (3 / h[i]) * (Y[i + 1] - Y[i]) - (3 / h[i - 1]) * (Y[i] - Y[i - 1]);
            }

            double[] c = new double[X.Length];
            double[] l = new double[X.Length];
            double[] mu = new double[X.Length];
            double[] z = new double[X.Length];

            l[0] = 1;
            mu[0] = 0;
            z[0] = 0;

            for (int i = 1; i < X.Length - 1; i++)
            {
                l[i] = 2 * (X[i + 1] - X[i - 1]) - h[i - 1] * mu[i - 1];
                mu[i] = h[i] / l[i];
                z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
            }

            l[X.Length - 1] = 1;
            z[X.Length - 1] = 0;
            c[X.Length - 1] = 0;

            for (int i = X.Length - 2; i >= 0; i--)
            {
                c[i] = z[i] - mu[i] * c[i + 1];
                B[i] = (Y[i + 1] - Y[i]) / h[i] - h[i] * (c[i + 1] + 2 * c[i]) / 3;
                D[i] = (c[i + 1] - c[i]) / (3 * h[i]);
            }

            for (int i = 0; i < X.Length - 1; i++)
            {
                if (x >= X[i] && x <= X[i + 1])
                {
                    double dx = x - X[i];
                    return (ushort)Math.Round(Y[i] + B[i] * dx + c[i] * Math.Pow(dx, 2) + D[i] * Math.Pow(dx, 3));
                }
            }

            return 0;
        }

        internal static Land GetHighestLand(GameObject go) {
            if (go == null) {
                return null;
            }

            Land highestLand = null;
            var tile = go;

            while (tile != null) {
                if (tile is Land land) {
                    highestLand = land;
                }

                tile = tile.TNext;
            }

            return highestLand;
        }

        internal static bool IsAnyWaterAbove(GameObject go) {
            if (go == null) {
                return false;
            }
            var tile = go;

            while (tile != null) {
                if (IsWater(tile) || IsBlackOrStars(tile)) {
                    return true;
                }

                tile = tile.TNext;
            }

            return false;
        }

        public static ushort SetPixel(int x, int y) {
            var test1 = CalculateNewIntensityFromHeight(-5);
            var test2 = CalculateNewIntensityFromHeight(0);
            var test3 = CalculateNewIntensityFromHeight(16);
            var test4 = CalculateNewIntensityFromHeight(30);
            var test5 = CalculateNewIntensityFromHeight(48);


            ushort intensity = 32767;
            ushort minIntensity = CalculateNewIntensityFromHeight(-40);
            ushort waterIntensity = CalculateNewIntensityFromHeight(-30);

            if (x >= MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0] || y >= MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1]) {
                return minIntensity;
            }

            var tiletest = World.Map.GetTile(x, y);

            //z 30
            if (x == 3469 && y == 2546) {
                int st = 1;

                //return 33411;
            }

            //z 48
            if (x == 3456 && y == 2507) {
                int st = 1;
                var test = CalculateNewIntensityFromHeight(tiletest.Z);
                //return 33795;
            }

            //z 16
            if (x == 1076 && y == 652) {
                int st = 1;
                var test = CalculateNewIntensityFromHeight(tiletest.Z);
                //return 33110;
            }

            //z -5 water 32662
            if (x == 1377 && y == 559) {
                int st = 1;
                //32662
            }

            if (x == 3489 && y == 2587) {
                int asdfff = 1;
            }

            for (int i = 0; i < 5; i++) {
                try {
                    var tile = World.Map.GetTile(x, y);

                    var highestLand = GetHighestLand(tile);

                    if (highestLand != null) {
                        var isWaterAbove = IsAnyWaterAbove(highestLand);

                        if (isWaterAbove) {
                            return waterIntensity;
                        }

                        return CalculateNewIntensityFromHeight(highestLand.Z);
                    }

                    if (tile != null) {
                        if(tile.TNext is Static stat) {

                            var highestLand2 = GetHighestLand(tile);

                            if (highestLand2 != null) {
                                var isWaterAbove = IsAnyWaterAbove(highestLand2);

                                if (isWaterAbove) {
                                    return waterIntensity;
                                }

                                return CalculateNewIntensityFromHeight(highestLand2.Z);
                            }

                            if (IsWater(stat)) {
                                return waterIntensity;
                            }
                        }


                        if (IsWater(tile) || IsBlackOrStars(tile)) {
                            return waterIntensity;
                        }

                        return CalculateNewIntensityFromHeight(tile.Z);

                    }
                    else {
                        int bob = 1;
                    }
                }
                catch (Exception) {
                    
                }
            }
            
            return intensity;
        }
    }
}
