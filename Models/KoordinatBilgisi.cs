namespace KibleYonu.Models
{
    public class KoordinatBilgisi
    {
        // Giriş değerleri
        public KoordinatSistemiTipi Sistem { get; set; } = KoordinatSistemiTipi.UTM;

        // UTM girişi
        public double Easting { get; set; }
        public double Northing { get; set; }
        public int UtmZone { get; set; } = 36;
        public bool KuzeyYarikure { get; set; } = true;
        public double MerkezMeridyen { get; set; } = 0; // 0 ise zone'dan hesaplanır

        // WGS84 / ED50 / ITRF96 girişi
        public double Enlem { get; set; }
        public double Boylam { get; set; }

        // Hesaplanmış WGS84 (tüm sistemlerden dönüştürülmüş)
        public double WGS84Enlem { get; set; }
        public double WGS84Boylam { get; set; }

        public bool GecerliMi()
        {
            if (Sistem == KoordinatSistemiTipi.UTM)
            {
                if (MerkezMeridyen != 0)
                    return Easting > 0 && Northing > 0;
                return Easting > 0 && Northing > 0 && UtmZone >= 1 && UtmZone <= 60;
            }

            return Enlem >= -90 && Enlem <= 90 && Boylam >= -180 && Boylam <= 180;
        }
    }
}
