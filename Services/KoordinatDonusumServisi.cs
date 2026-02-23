using System;
using KibleYonu.Models;

namespace KibleYonu.Services
{
    public class KoordinatDonusumServisi
    {
        // WGS84 elipsoid parametreleri
        private const double WGS84_a = 6378137.0;
        private const double WGS84_f = 1.0 / 298.257223563;

        /// <summary>
        /// Verilen koordinat bilgisini WGS84'e dönüştürür.
        /// </summary>
        public void WGS84eDonustur(KoordinatBilgisi koordinat)
        {
            switch (koordinat.Sistem)
            {
                case KoordinatSistemiTipi.WGS84:
                    koordinat.WGS84Enlem = koordinat.Enlem;
                    koordinat.WGS84Boylam = koordinat.Boylam;
                    break;

                case KoordinatSistemiTipi.UTM:
                    double utmLat, utmLon;
                    if (koordinat.MerkezMeridyen != 0)
                        TmToLatLon(koordinat.Easting, koordinat.Northing,
                            koordinat.MerkezMeridyen, koordinat.KuzeyYarikure,
                            out utmLat, out utmLon);
                    else
                        UtmToLatLon(koordinat.Easting, koordinat.Northing,
                            koordinat.UtmZone, koordinat.KuzeyYarikure,
                            out utmLat, out utmLon);
                    koordinat.WGS84Enlem = utmLat;
                    koordinat.WGS84Boylam = utmLon;
                    break;

                case KoordinatSistemiTipi.ED50:
                    ED50ToWGS84(koordinat.Enlem, koordinat.Boylam,
                        out double wgsLat, out double wgsLon);
                    koordinat.WGS84Enlem = wgsLat;
                    koordinat.WGS84Boylam = wgsLon;
                    break;

                case KoordinatSistemiTipi.ITRF96:
                    // ITRF96 ≈ WGS84 (mm seviyesinde fark, pratik olarak aynı)
                    koordinat.WGS84Enlem = koordinat.Enlem;
                    koordinat.WGS84Boylam = koordinat.Boylam;
                    break;
            }
        }

        /// <summary>
        /// Boylamdan UTM zone otomatik tespit.
        /// </summary>
        public static int BoylamdanUtmZone(double boylam)
        {
            return (int)Math.Floor((boylam + 180.0) / 6.0) + 1;
        }

        /// <summary>
        /// Türkiye TM dilim sistemine göre merkez meridyen tespit.
        /// Dilimler: 27°, 30°, 33°, 36°, 39°, 42°, 45° (3° aralıklı)
        /// Türkiye dışı boylamlar için standart UTM CM kullanır.
        /// </summary>
        public static int TurkiyeTmMeridyen(double boylam)
        {
            if (boylam >= 25.5 && boylam <= 46.5)
                return (int)(Math.Floor((boylam - 25.5) / 3.0) * 3 + 27);

            // Türkiye dışı → standart UTM zone CM
            int zone = BoylamdanUtmZone(boylam);
            return (zone - 1) * 6 - 180 + 3;
        }

        /// <summary>
        /// Transverse Mercator → WGS84 (Enlem/Boylam) dönüşümü.
        /// Merkez meridyen (derece) bazlı — hem UTM hem TM dilimlerini destekler.
        /// </summary>
        public static void TmToLatLon(double easting, double northing,
            double merkezMeridyen, bool northern, out double lat, out double lon)
        {
            double a = WGS84_a;
            double f = WGS84_f;
            double e2 = 2 * f - f * f;
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));

            double k0 = 0.9996;
            double x = easting - 500000.0;
            double y = northern ? northing : northing - 10000000.0;

            double m = y / k0;
            double mu = m / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));

            double phi1 = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                       + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                       + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);

            double sinPhi1 = Math.Sin(phi1);
            double cosPhi1 = Math.Cos(phi1);
            double tanPhi1 = Math.Tan(phi1);
            double n1 = a / Math.Sqrt(1 - e2 * sinPhi1 * sinPhi1);
            double t1 = tanPhi1 * tanPhi1;
            double c1 = e2 / (1 - e2) * cosPhi1 * cosPhi1;
            double r1 = a * (1 - e2) / Math.Pow(1 - e2 * sinPhi1 * sinPhi1, 1.5);
            double d2 = x / (n1 * k0);

            lat = phi1 - (n1 * tanPhi1 / r1) * (d2 * d2 / 2
                 - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * e2 / (1 - e2)) * d2 * d2 * d2 * d2 / 24
                 + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * e2 / (1 - e2) - 3 * c1 * c1)
                   * d2 * d2 * d2 * d2 * d2 * d2 / 720);

            lon = merkezMeridyen * Math.PI / 180.0
                 + (d2 - (1 + 2 * t1 + c1) * d2 * d2 * d2 / 6
                 + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * e2 / (1 - e2) + 24 * t1 * t1)
                   * d2 * d2 * d2 * d2 * d2 / 120) / cosPhi1;

            lat = lat * 180.0 / Math.PI;
            lon = lon * 180.0 / Math.PI;
        }

        /// <summary>
        /// UTM → WGS84 (Enlem/Boylam) dönüşümü. Geriye uyumluluk için korunmuştur.
        /// </summary>
        public static void UtmToLatLon(double easting, double northing, int zone, bool northern,
            out double lat, out double lon)
        {
            double cm = (zone - 1) * 6 - 180 + 3;
            TmToLatLon(easting, northing, cm, northern, out lat, out lon);
        }

        /// <summary>
        /// ED50 → WGS84 Molodensky dönüşümü.
        /// </summary>
        public static void ED50ToWGS84(double ed50Lat, double ed50Lon, out double wgsLat, out double wgsLon)
        {
            double dX = KoordinatSistemiInfo.ED50_DX;
            double dY = KoordinatSistemiInfo.ED50_DY;
            double dZ = KoordinatSistemiInfo.ED50_DZ;

            // ED50 (International 1924) elipsoid
            double a = 6378388.0;
            double f = 1.0 / 297.0;
            double da = KoordinatSistemiInfo.ED50_DA;
            double df = KoordinatSistemiInfo.ED50_DF;

            double lat = ed50Lat * Math.PI / 180.0;
            double lon = ed50Lon * Math.PI / 180.0;

            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double sinLon = Math.Sin(lon);
            double cosLon = Math.Cos(lon);

            double e2 = 2 * f - f * f;
            double Rn = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
            double Rm = a * (1 - e2) / Math.Pow(1 - e2 * sinLat * sinLat, 1.5);

            double dLat = (-dX * sinLat * cosLon - dY * sinLat * sinLon + dZ * cosLat
                          + da * (Rn * e2 * sinLat * cosLat) / a
                          + df * (Rm * a / (1 - f) + Rn * (1 - f) / 1) * sinLat * cosLat)
                          / (Rm + 0);

            // Basitleştirilmiş Molodensky
            dLat = (-dX * sinLat * cosLon - dY * sinLat * sinLon + dZ * cosLat
                   + (a * df + f * da) * 2 * Rn * sinLat * cosLat / a)
                   / Rm;

            double dLon = (-dX * sinLon + dY * cosLon) / (Rn * cosLat);

            wgsLat = ed50Lat + dLat * 180.0 / Math.PI;
            wgsLon = ed50Lon + dLon * 180.0 / Math.PI;
        }
    }
}
