using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace KibleYonu.Services
{
    public class SehirKaydi
    {
        [JsonProperty("name")]
        public string Ad { get; set; } = "";

        [JsonProperty("province")]
        public string Il { get; set; } = "";

        [JsonProperty("country")]
        public string Ulke { get; set; } = "";

        [JsonProperty("lat")]
        public double Enlem { get; set; }

        [JsonProperty("lon")]
        public double Boylam { get; set; }

        [JsonProperty("nameAscii")]
        public string AdAscii { get; set; } = "";

        public string TamAd => string.IsNullOrEmpty(Il) ? $"{Ad}, {Ulke}" : $"{Ad}, {Il}";
    }

    public class KonumAramaServisi
    {
        private List<SehirKaydi> _sehirler;
        private bool _yuklendi = false;

        public KonumAramaServisi()
        {
            SehirleriYukle();
        }

        private void SehirleriYukle()
        {
            if (_yuklendi) return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "KibleYonu.Resources.districts.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        _sehirler = new List<SehirKaydi>();
                        return;
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        _sehirler = JsonConvert.DeserializeObject<List<SehirKaydi>>(json)
                                    ?? new List<SehirKaydi>();
                    }
                }

                _yuklendi = true;
            }
            catch
            {
                _sehirler = new List<SehirKaydi>();
            }
        }

        /// <summary>
        /// Fuzzy şehir arama (Türkçe karakter normalizasyonu dahil).
        /// </summary>
        public List<SehirKaydi> Ara(string sorgu, int maxSonuc = 20)
        {
            if (string.IsNullOrWhiteSpace(sorgu) || _sehirler == null)
                return new List<SehirKaydi>();

            string normalSorgu = NormalizeMetin(sorgu);

            return _sehirler
                .Where(s => NormalizeMetin(s.Ad).Contains(normalSorgu)
                         || NormalizeMetin(s.AdAscii).Contains(normalSorgu)
                         || NormalizeMetin(s.Il).Contains(normalSorgu)
                         || NormalizeMetin(s.Ulke).Contains(normalSorgu)
                         || NormalizeMetin(s.TamAd).Contains(normalSorgu))
                .OrderByDescending(s => NormalizeMetin(s.Ad).StartsWith(normalSorgu))
                .ThenBy(s => s.Ad)
                .Take(maxSonuc)
                .ToList();
        }

        /// <summary>
        /// Verilen koordinata en yakın şehri bulur.
        /// </summary>
        public SehirKaydi EnYakinSehir(double enlem, double boylam)
        {
            if (_sehirler == null || _sehirler.Count == 0) return null;

            return _sehirler
                .OrderBy(s => Math.Pow(s.Enlem - enlem, 2) + Math.Pow(s.Boylam - boylam, 2))
                .FirstOrDefault();
        }

        public List<SehirKaydi> TumSehirler => _sehirler ?? new List<SehirKaydi>();

        private static string NormalizeMetin(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return "";

            return metin
                .Replace("\u0130", "I").Replace("\u0131", "I")   // İ ı
                .Replace("\u011e", "G").Replace("\u011f", "G")   // Ğ ğ
                .Replace("\u00dc", "U").Replace("\u00fc", "U")   // Ü ü
                .Replace("\u015e", "S").Replace("\u015f", "S")   // Ş ş
                .Replace("\u00d6", "O").Replace("\u00f6", "O")   // Ö ö
                .Replace("\u00c7", "C").Replace("\u00e7", "C")   // Ç ç
                .ToUpperInvariant()
                .Trim();
        }
    }
}
