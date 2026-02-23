namespace KibleYonu.Models
{
    public enum KoordinatSistemiTipi
    {
        WGS84,
        UTM,
        ED50,
        ITRF96
    }

    public static class KoordinatSistemiInfo
    {
        public static string Aciklama(KoordinatSistemiTipi tip)
        {
            switch (tip)
            {
                case KoordinatSistemiTipi.WGS84: return "WGS84 (Enlem/Boylam)";
                case KoordinatSistemiTipi.UTM: return "UTM (Easting/Northing)";
                case KoordinatSistemiTipi.ED50: return "ED50 (Eski Türkiye Datumu)";
                case KoordinatSistemiTipi.ITRF96: return "ITRF96 (Türkiye Resmi)";
                default: return tip.ToString();
            }
        }

        // ED50 → WGS84 Molodensky parametreleri
        public const double ED50_DX = -87.0;
        public const double ED50_DY = -98.0;
        public const double ED50_DZ = -121.0;
        public const double ED50_DA = -251.0;  // a(ED50) - a(WGS84)
        public const double ED50_DF = -1.41927E-05; // f(ED50) - f(WGS84)
    }
}
