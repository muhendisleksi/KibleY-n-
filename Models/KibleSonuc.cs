namespace KibleYonu.Models
{
    public class KibleSonuc
    {
        public double KibleAcisi { get; set; }
        public double MesafeKm { get; set; }
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public double ManyetikSapma { get; set; }
        public string KonumAdi { get; set; } = "";

        public string AciMetni => $"{KibleAcisi:F1}°";
        public string MesafeMetni => $"{MesafeKm:F0} km";
        public string KoordinatMetni => $"{Enlem:F4}°N  {Boylam:F4}°E";
        public string OzetMetni => $"Kıble: {AciMetni} — {MesafeMetni}";
    }
}
