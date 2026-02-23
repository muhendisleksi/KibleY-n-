using System;

namespace KibleYonu.Models
{
    public class KonumGecmisi
    {
        public string KonumAdi { get; set; } = "";
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public double KibleAcisi { get; set; }
        public double MesafeKm { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;

        public string OzetMetni => $"{KonumAdi} — {KibleAcisi:F1}° — {MesafeKm:F0} km";
    }
}
