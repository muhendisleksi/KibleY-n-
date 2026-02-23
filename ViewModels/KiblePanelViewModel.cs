using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KibleYonu.Models;
using KibleYonu.Services;

namespace KibleYonu.ViewModels
{
    public class KiblePanelViewModel : ViewModelBase
    {
        private readonly KibleHesaplamaServisi _hesapServisi;
        private readonly KoordinatDonusumServisi _donusumServisi;
        private readonly KonumAramaServisi _aramaServisi;
        private readonly ManyetikSapmaServisi _sapmaServisi;

        public KiblePanelViewModel()
        {
            _hesapServisi = new KibleHesaplamaServisi();
            _donusumServisi = new KoordinatDonusumServisi();
            _aramaServisi = new KonumAramaServisi();
            _sapmaServisi = new ManyetikSapmaServisi();

            Gecmis = new ObservableCollection<KonumGecmisi>();
            AramaSonuclari = new ObservableCollection<SehirKaydi>();

            HesaplaKomutu = new RelayCommand(Hesapla, () => true);
            YerlestirKomutu = new RelayCommand(PusulaYerlestir, () => SonucVar);
            CizimdanSecKomutu = new RelayCommand(CizimdanSec);
            SonucKopyalaKomutu = new RelayCommand(SonucKopyala, () => SonucVar);
            GecmisTemizleKomutu = new RelayCommand(GecmisTemizle);
        }

        // ═══ KOORDİNAT SİSTEMİ ═══

        private KoordinatSistemiTipi _seciliSistem = KoordinatSistemiTipi.UTM;
        public KoordinatSistemiTipi SeciliSistem
        {
            get => _seciliSistem;
            set
            {
                if (SetProperty(ref _seciliSistem, value))
                {
                    OnPropertyChanged(nameof(UtmGorunur));
                    OnPropertyChanged(nameof(EnlemBoylamGorunur));
                }
            }
        }

        public bool UtmGorunur => SeciliSistem == KoordinatSistemiTipi.UTM;
        public bool EnlemBoylamGorunur => SeciliSistem != KoordinatSistemiTipi.UTM;

        // ═══ UTM GİRİŞ ═══

        private string _eastingMetni = "";
        public string EastingMetni
        {
            get => _eastingMetni;
            set => SetProperty(ref _eastingMetni, value);
        }

        private string _northingMetni = "";
        public string NorthingMetni
        {
            get => _northingMetni;
            set => SetProperty(ref _northingMetni, value);
        }

        private int _utmZone = 36;
        public int UtmZone
        {
            get => _utmZone;
            set => SetProperty(ref _utmZone, value);
        }

        private bool _kuzeyYarikure = true;
        public bool KuzeyYarikure
        {
            get => _kuzeyYarikure;
            set => SetProperty(ref _kuzeyYarikure, value);
        }

        // ═══ PROJEKSİYON MODU ═══

        private bool _tmModu = false;
        public bool TmModu
        {
            get => _tmModu;
            set
            {
                if (SetProperty(ref _tmModu, value))
                {
                    OnPropertyChanged(nameof(UtmZoneGorunur));
                    OnPropertyChanged(nameof(MerkezMeridyenGorunur));
                }
            }
        }

        private int _merkezMeridyen = 30; // Türkiye varsayılanı
        public int MerkezMeridyen
        {
            get => _merkezMeridyen;
            set => SetProperty(ref _merkezMeridyen, value);
        }

        public bool UtmZoneGorunur => !TmModu;
        public bool MerkezMeridyenGorunur => TmModu;

        // ═══ ENLEM/BOYLAM GİRİŞ ═══

        private string _enlemMetni = "";
        public string EnlemMetni
        {
            get => _enlemMetni;
            set => SetProperty(ref _enlemMetni, value);
        }

        private string _boylamMetni = "";
        public string BoylamMetni
        {
            get => _boylamMetni;
            set => SetProperty(ref _boylamMetni, value);
        }

        // ═══ ŞEHİR ARAMA ═══

        private string _aramaMetni = "";
        public string AramaMetni
        {
            get => _aramaMetni;
            set
            {
                if (SetProperty(ref _aramaMetni, value))
                    SehirAra();
            }
        }

        private ObservableCollection<SehirKaydi> _aramaSonuclari;
        public ObservableCollection<SehirKaydi> AramaSonuclari
        {
            get => _aramaSonuclari;
            set => SetProperty(ref _aramaSonuclari, value);
        }

        private SehirKaydi _seciliSehir;
        public SehirKaydi SeciliSehir
        {
            get => _seciliSehir;
            set
            {
                if (SetProperty(ref _seciliSehir, value) && value != null)
                    SehirSec(value);
            }
        }

        private bool _aramaPaneliAcik = false;
        public bool AramaPaneliAcik
        {
            get => _aramaPaneliAcik;
            set => SetProperty(ref _aramaPaneliAcik, value);
        }

        // ═══ PUSULA AYARLARI ═══

        private double _yaricap = 100.0;
        public double Yaricap
        {
            get => _yaricap;
            set => SetProperty(ref _yaricap, value);
        }

        private string _yaricapMetni = "100";
        public string YaricapMetni
        {
            get => _yaricapMetni;
            set
            {
                if (SetProperty(ref _yaricapMetni, value))
                {
                    if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double r) && r > 0)
                        _yaricap = r;
                }
            }
        }

        private DetaySeviyesi _detaySeviyesi = DetaySeviyesi.Normal;
        public DetaySeviyesi DetaySeviyesi
        {
            get => _detaySeviyesi;
            set => SetProperty(ref _detaySeviyesi, value);
        }

        private bool _manyetikSapmaGoster = false;
        public bool ManyetikSapmaGoster
        {
            get => _manyetikSapmaGoster;
            set => SetProperty(ref _manyetikSapmaGoster, value);
        }

        private bool _bilgiPaneliGoster = true;
        public bool BilgiPaneliGoster
        {
            get => _bilgiPaneliGoster;
            set => SetProperty(ref _bilgiPaneliGoster, value);
        }

        // ═══ SONUÇLAR ═══

        private KibleSonuc _sonuc;
        public KibleSonuc Sonuc
        {
            get => _sonuc;
            set
            {
                if (SetProperty(ref _sonuc, value))
                {
                    OnPropertyChanged(nameof(SonucVar));
                    OnPropertyChanged(nameof(KibleAcisiGoruntusu));
                    OnPropertyChanged(nameof(MesafeGoruntusu));
                    OnPropertyChanged(nameof(KoordinatGoruntusu));
                    OnPropertyChanged(nameof(ManyetikSapmaGoruntusu));
                    OnPropertyChanged(nameof(MiniPusulaAcisi));
                }
            }
        }

        public bool SonucVar => _sonuc != null;
        public string KibleAcisiGoruntusu => _sonuc?.AciMetni ?? "—";
        public string MesafeGoruntusu => _sonuc?.MesafeMetni ?? "—";
        public string KoordinatGoruntusu => _sonuc?.KoordinatMetni ?? "—";
        public double MiniPusulaAcisi => _sonuc?.KibleAcisi ?? 0;

        public string ManyetikSapmaGoruntusu
        {
            get
            {
                if (_sonuc == null) return "";
                return _sapmaServisi.SapmaMetni(_sonuc.ManyetikSapma);
            }
        }

        private string _hataMesaji = "";
        public string HataMesaji
        {
            get => _hataMesaji;
            set => SetProperty(ref _hataMesaji, value);
        }

        // ═══ GEÇMİŞ ═══

        public ObservableCollection<KonumGecmisi> Gecmis { get; }

        // ═══ KOMUTLAR ═══

        public ICommand HesaplaKomutu { get; }
        public ICommand YerlestirKomutu { get; }
        public ICommand CizimdanSecKomutu { get; }
        public ICommand SonucKopyalaKomutu { get; }
        public ICommand GecmisTemizleKomutu { get; }

        // Dışarıdan set edilecek action (UIManager tarafından)
        // Parametreler: yaricap, detay, bilgiPaneli, merkezMeridyen, kuzeyYarikure
        public Action<double, DetaySeviyesi, bool, int, bool> PusulaYerlestirAction { get; set; }
        // Parametreler: callback, merkezMeridyen, kuzeyYarikure
        public Action<Action<double, double>, int, bool> CizimdanSecAction { get; set; }

        // ═══ HESAPLAMA ═══

        private void Hesapla()
        {
            HataMesaji = "";

            try
            {
                var koordinat = new KoordinatBilgisi { Sistem = SeciliSistem };

                if (SeciliSistem == KoordinatSistemiTipi.UTM)
                {
                    if (!double.TryParse(EastingMetni, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double e) || e <= 0)
                    {
                        HataMesaji = "Geçersiz Easting değeri.";
                        return;
                    }
                    if (!double.TryParse(NorthingMetni, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double n) || n <= 0)
                    {
                        HataMesaji = "Geçersiz Northing değeri.";
                        return;
                    }

                    koordinat.Easting = e;
                    koordinat.Northing = n;
                    koordinat.KuzeyYarikure = KuzeyYarikure;

                    if (TmModu)
                        koordinat.MerkezMeridyen = MerkezMeridyen;
                    else
                        koordinat.UtmZone = UtmZone;
                }
                else
                {
                    if (!double.TryParse(EnlemMetni, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double lat))
                    {
                        HataMesaji = "Geçersiz enlem değeri.";
                        return;
                    }
                    if (!double.TryParse(BoylamMetni, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double lon))
                    {
                        HataMesaji = "Geçersiz boylam değeri.";
                        return;
                    }

                    koordinat.Enlem = lat;
                    koordinat.Boylam = lon;
                }

                _donusumServisi.WGS84eDonustur(koordinat);

                var sonuc = _hesapServisi.Hesapla(koordinat.WGS84Enlem, koordinat.WGS84Boylam);

                // En yakın şehri bul
                var enYakin = _aramaServisi.EnYakinSehir(koordinat.WGS84Enlem, koordinat.WGS84Boylam);
                if (enYakin != null)
                {
                    sonuc.KonumAdi = enYakin.TamAd;
                    _aramaMetni = enYakin.TamAd;
                    OnPropertyChanged(nameof(AramaMetni));
                    AramaPaneliAcik = false;
                }

                // Manyetik sapma
                sonuc.ManyetikSapma = _sapmaServisi.SapmaHesapla(koordinat.WGS84Enlem, koordinat.WGS84Boylam);

                Sonuc = sonuc;

                // Geçmişe ekle
                Gecmis.Insert(0, new KonumGecmisi
                {
                    KonumAdi = sonuc.KonumAdi,
                    Enlem = sonuc.Enlem,
                    Boylam = sonuc.Boylam,
                    KibleAcisi = sonuc.KibleAcisi,
                    MesafeKm = sonuc.MesafeKm
                });

                if (Gecmis.Count > 20) Gecmis.RemoveAt(Gecmis.Count - 1);
            }
            catch (Exception ex)
            {
                HataMesaji = $"Hesaplama hatası: {ex.Message}";
            }
        }

        private void PusulaYerlestir()
        {
            if (Sonuc == null) return;

            int merkezMeridyen;
            bool kuzeyYk;
            if (SeciliSistem == KoordinatSistemiTipi.UTM)
            {
                merkezMeridyen = TmModu ? MerkezMeridyen : (UtmZone - 1) * 6 - 180 + 3;
                kuzeyYk = KuzeyYarikure;
            }
            else
            {
                // WGS84/ED50/ITRF96 → boylamdan otomatik UTM CM hesapla
                int zone = (int)Math.Floor((Sonuc.Boylam + 180) / 6) + 1;
                merkezMeridyen = (zone - 1) * 6 - 180 + 3;
                kuzeyYk = Sonuc.Enlem >= 0;
            }

            PusulaYerlestirAction?.Invoke(
                _yaricap, _detaySeviyesi, _bilgiPaneliGoster, merkezMeridyen, kuzeyYk);
        }

        private void CizimdanSec()
        {
            CizimdanSecAction?.Invoke((lat, lon) =>
            {
                SeciliSistem = KoordinatSistemiTipi.WGS84;
                EnlemMetni = lat.ToString("F6");
                BoylamMetni = lon.ToString("F6");
                Hesapla();
            }, MerkezMeridyen, KuzeyYarikure);
        }

        private void SonucKopyala()
        {
            if (Sonuc == null) return;
            try
            {
                string metin = $"Kıble Açısı: {Sonuc.AciMetni}\n"
                             + $"Mesafe: {Sonuc.MesafeMetni}\n"
                             + $"Koordinat: {Sonuc.KoordinatMetni}\n"
                             + $"Konum: {Sonuc.KonumAdi}";
                Clipboard.SetText(metin);
            }
            catch { /* Pano erişim hatası */ }
        }

        private void GecmisTemizle()
        {
            Gecmis.Clear();
        }

        private void SehirAra()
        {
            if (string.IsNullOrWhiteSpace(AramaMetni) || AramaMetni.Length < 2)
            {
                AramaSonuclari.Clear();
                AramaPaneliAcik = false;
                return;
            }

            var sonuclar = _aramaServisi.Ara(AramaMetni, 10);
            AramaSonuclari.Clear();
            foreach (var s in sonuclar)
                AramaSonuclari.Add(s);

            AramaPaneliAcik = AramaSonuclari.Count > 0;
        }

        private void SehirSec(SehirKaydi sehir)
        {
            SeciliSistem = KoordinatSistemiTipi.WGS84;
            EnlemMetni = sehir.Enlem.ToString("F4");
            BoylamMetni = sehir.Boylam.ToString("F4");
            AramaMetni = sehir.TamAd;
            AramaPaneliAcik = false;

            // Türkiye TM dilimine göre merkez meridyen otomatik ayarla
            MerkezMeridyen = KoordinatDonusumServisi.TurkiyeTmMeridyen(sehir.Boylam);
            UtmZone = KoordinatDonusumServisi.BoylamdanUtmZone(sehir.Boylam);

            Hesapla();
        }
    }
}
