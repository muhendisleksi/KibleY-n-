using System;
using KibleYonu.Models;

namespace KibleYonu.Services
{
    public class KibleHesaplamaServisi
    {
        private const double KabeEnlem = 21.42252;
        private const double KabeBoylam = 39.82621;
        private const double DunyaYaricapKm = 6371.0;

        /// <summary>
        /// WGS84 enlem/boylam'dan kıble açısı ve Kabe mesafesi hesaplar.
        /// Great circle forward azimuth yöntemi kullanır.
        /// </summary>
        public KibleSonuc Hesapla(double enlem, double boylam)
        {
            double kibleAcisi = HesaplaKibleAcisi(enlem, boylam);
            double mesafe = HesaplaKabeUzakligi(enlem, boylam);

            return new KibleSonuc
            {
                KibleAcisi = kibleAcisi,
                MesafeKm = mesafe,
                Enlem = enlem,
                Boylam = boylam
            };
        }

        /// <summary>
        /// Büyük daire (great circle) forward azimuth ile kıble açısı.
        /// Kuzeyden saat yönünde derece cinsinden.
        /// </summary>
        public static double HesaplaKibleAcisi(double enlem, double boylam)
        {
            double lat1 = enlem * Math.PI / 180.0;
            double lon1 = boylam * Math.PI / 180.0;
            double lat2 = KabeEnlem * Math.PI / 180.0;
            double lon2 = KabeBoylam * Math.PI / 180.0;

            double dLon = lon2 - lon1;
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double bearing = Math.Atan2(y, x);
            double bearingDerece = bearing * 180.0 / Math.PI;
            return (bearingDerece + 360.0) % 360.0;
        }

        /// <summary>
        /// Haversine formülü ile Kabe mesafesi (km).
        /// </summary>
        public static double HesaplaKabeUzakligi(double enlem, double boylam)
        {
            double lat1 = enlem * Math.PI / 180.0;
            double lon1 = boylam * Math.PI / 180.0;
            double lat2 = KabeEnlem * Math.PI / 180.0;
            double lon2 = KabeBoylam * Math.PI / 180.0;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return DunyaYaricapKm * c;
        }
    }
}
