namespace KibleYonu.Models
{
    public enum DetaySeviyesi
    {
        Basit,
        Normal,
        Detayli
    }

    public class PusulaAyarlari
    {
        public double Yaricap { get; set; } = 100.0;
        public DetaySeviyesi Detay { get; set; } = DetaySeviyesi.Normal;
        public bool ManyetikSapmaGoster { get; set; } = false;
        public bool BilgiPaneliGoster { get; set; } = true;
    }
}
