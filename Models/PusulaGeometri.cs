using System;
using Autodesk.AutoCAD.Geometry;

namespace KibleYonu.Models
{
    /// <summary>
    /// Modern pusula tarzında kıble yönü çizimi için geometri bilgilerini taşır.
    /// </summary>
    public struct PusulaGeometri
    {
        // ── Oran sabitleri ─────────────────────────────
        public const double DisHalka = 1.00;
        public const double IcHalka = 0.93;
        public const double MerkezDaire = 0.12;
        public const double MerkezNokta = 0.018;

        public const double TickDis = 0.93;
        public const double TickBuyukIc = 0.84;
        public const double TickOrtaIc = 0.87;
        public const double TickKucukIc = 0.90;

        public const double YonEtiket = 0.76;
        public const double RefCizgiIc = 0.14;
        public const double RefCizgiDis = 0.72;

        public const double OkBaslangic = 0.14;
        public const double KibleGovdeBitis = 0.70;
        public const double KibleUcNokta = 0.88;

        public const double KuzeyGovdeBitis = 0.58;
        public const double KuzeyUcNokta = 0.70;

        public const double AciYayR = 0.32;
        public const double DisEtiketR = 1.08;

        public const double KucukMetin = 0.035;
        public const double NormalMetin = 0.050;
        public const double BuyukMetin = 0.075;

        // ── Renkler (AutoCAD Color Index) ──────────────
        public const short RenkKible = 1;      // Kırmızı
        public const short RenkKuzey = 5;      // Mavi
        public const short RenkHalka = 7;      // Beyaz
        public const short RenkTickBuyuk = 7;  // Beyaz
        public const short RenkTickKucuk = 8;  // Gri
        public const short RenkYon = 9;        // Açık gri
        public const short RenkRefCizgi = 8;   // Gri
        public const short RenkAciYay = 2;     // Sarı
        public const short RenkKabe = 30;      // Turuncu
        public const short RenkBilgi = 7;      // Beyaz

        // ── Hesaplanan değerler ────────────────────────
        public Point3d Merkez;
        public double Yaricap;
        public double KibleAcisi;
        public double AcadAcisiRad;
        public double UzaklikKm;
        public double Enlem;
        public double Boylam;
        public string KonumAdi;

        public string Etiket => $"KIBLE {KibleAcisi:F1}° — {UzaklikKm:F0} km";

        public static PusulaGeometri Hesapla(Point3d merkez, double yaricap, double enlem, double boylam)
        {
            var hesap = new Services.KibleHesaplamaServisi();
            var sonuc = hesap.Hesapla(enlem, boylam);

            var g = new PusulaGeometri
            {
                Merkez = merkez,
                Yaricap = yaricap,
                Enlem = enlem,
                Boylam = boylam,
                KibleAcisi = sonuc.KibleAcisi,
                UzaklikKm = sonuc.MesafeKm,
                AcadAcisiRad = (90.0 - sonuc.KibleAcisi) * Math.PI / 180.0,
                KonumAdi = ""
            };

            return g;
        }

        public static Point3d Polar(Point3d bp, double angle, double dist)
        {
            return new Point3d(
                bp.X + dist * Math.Cos(angle),
                bp.Y + dist * Math.Sin(angle),
                bp.Z);
        }
    }
}
