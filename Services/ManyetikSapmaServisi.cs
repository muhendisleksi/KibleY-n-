using System;

namespace KibleYonu.Services
{
    /// <summary>
    /// Basitleştirilmiş manyetik sapma hesabı (dipol modeli).
    /// Sadece bilgilendirme amaçlı — hesaplama gerçek kuzeye göre yapılır.
    /// </summary>
    public class ManyetikSapmaServisi
    {
        // WMM2020 dipol katsayıları (basitleştirilmiş)
        // Manyetik kutup konumu (yaklaşık 2025)
        private const double ManyetikKutupEnlem = 80.65;
        private const double ManyetikKutupBoylam = -72.68;

        /// <summary>
        /// Verilen konumdaki yaklaşık manyetik sapma (declination) değeri.
        /// Pozitif: doğu sapması, Negatif: batı sapması.
        /// Doğruluk: ±2-3° (bilgilendirme amaçlı yeterli).
        /// </summary>
        public double SapmaHesapla(double enlem, double boylam)
        {
            // Basit dipol modeli
            double latRad = enlem * Math.PI / 180.0;
            double lonRad = boylam * Math.PI / 180.0;
            double mLatRad = ManyetikKutupEnlem * Math.PI / 180.0;
            double mLonRad = ManyetikKutupBoylam * Math.PI / 180.0;

            // Manyetik kutup ile coğrafi kutup arasındaki farka dayalı hesap
            double cosP = Math.Sin(mLatRad) * Math.Sin(latRad)
                        + Math.Cos(mLatRad) * Math.Cos(latRad) * Math.Cos(lonRad - mLonRad);

            double sinP = Math.Sqrt(1 - cosP * cosP);
            if (Math.Abs(sinP) < 1e-10) return 0;

            double declination = Math.Asin(
                Math.Cos(mLatRad) * Math.Sin(lonRad - mLonRad) / sinP
            ) * 180.0 / Math.PI;

            return Math.Round(declination, 1);
        }

        public string SapmaMetni(double sapma)
        {
            if (Math.Abs(sapma) < 0.1) return "~0° (ihmal edilebilir)";
            string yon = sapma > 0 ? "D" : "B";
            return $"{Math.Abs(sapma):F1}° {yon}";
        }
    }
}
