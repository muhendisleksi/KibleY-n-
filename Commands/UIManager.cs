using System;
using System.Linq;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;
using KibleYonu.Models;
using KibleYonu.Services;
using KibleYonu.ViewModels;
using KibleYonu.Views;

namespace KibleYonu.Commands
{
    public class UIManager
    {
        private static PaletteSet _paletteSet;
        private static KiblePanelViewModel _viewModel;

        public static void PanelGoster()
        {
            if (_paletteSet == null)
                PanelOlustur();

            _paletteSet.Visible = true;
        }

        public static void PanelKapat()
        {
            if (_paletteSet != null)
                _paletteSet.Visible = false;
        }

        public static void PanelToggle()
        {
            if (_paletteSet == null)
                PanelOlustur();

            _paletteSet.Visible = !_paletteSet.Visible;
        }

        private static void PanelOlustur()
        {
            _viewModel = new KiblePanelViewModel();

            // Pusula yerleştirme action'ı
            _viewModel.PusulaYerlestirAction = PusulaYerlestir;
            _viewModel.CizimdanSecAction = CizimdanSec;

            var panel = new KiblePanelControl();
            panel.DataContext = _viewModel;

            ElementHost host = new ElementHost
            {
                Child = panel,
                Dock = System.Windows.Forms.DockStyle.Fill
            };

            _paletteSet = new PaletteSet("Kible Yonu",
                new Guid("B7E3F1A2-5C4D-4E6F-8A9B-0C1D2E3F4A5B"));
            _paletteSet.Style = PaletteSetStyles.ShowAutoHideButton
                              | PaletteSetStyles.ShowCloseButton
                              | PaletteSetStyles.Snappable;
            _paletteSet.MinimumSize = new System.Drawing.Size(280, 400);
            _paletteSet.DockEnabled = DockSides.Left | DockSides.Right;

            _paletteSet.Add("Kible", host);
        }

        private static void PusulaYerlestir(double yaricap,
            DetaySeviyesi detay, bool bilgiPaneli, int merkezMeridyen, bool kuzeyYarikure)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Jig TM modunda — fare hareket ettikçe kıble oku dinamik döner
                var jig = new KibleJig(yaricap, merkezMeridyen, kuzeyYarikure);
                PromptResult pr = ed.Drag(jig);

                if (pr.Status == PromptStatus.OK)
                {
                    PusulaGeometri geo = jig.Geometri;
                    if (_viewModel?.Sonuc != null)
                        geo.KonumAdi = _viewModel.Sonuc.KonumAdi;

                    var cizimServisi = new PusulaCizimServisi();
                    cizimServisi.PusulaCiz(db, geo, detay, bilgiPaneli);

                    ed.WriteMessage($"\n  Kible pusulasi yerlestirildi.");
                    ed.WriteMessage($"\n  Aci: {geo.KibleAcisi:F1}° — Mesafe: {geo.UzaklikKm:F0} km\n");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nHata: {ex.Message}");
            }
        }

        private static int _sonCm = 0; // Son kullanılan CM hatırlanır

        private static void CizimdanSec(Action<double, double> callback, int merkezMeridyen, bool kuzeyYarikure)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                // İlk kullanımda paneldeki değer, sonrakilerde son girilen değer
                int varsayilan = _sonCm > 0 ? _sonCm : merkezMeridyen;

                ed.WriteMessage("\n  27=Trakya  30=Marmara/Ege  33=IcAnadolu  36=DoguKaradeniz  39=DoguAnadolu  42/45=Sinir");

                PromptKeywordOptions cmOpt = new PromptKeywordOptions(
                    "\nMerkez Meridyen: ");
                cmOpt.Keywords.Add("27");
                cmOpt.Keywords.Add("30");
                cmOpt.Keywords.Add("33");
                cmOpt.Keywords.Add("36");
                cmOpt.Keywords.Add("39");
                cmOpt.Keywords.Add("42");
                cmOpt.Keywords.Add("45");
                cmOpt.Keywords.Default = varsayilan.ToString();
                cmOpt.AllowNone = true;

                PromptResult cmRes = ed.GetKeywords(cmOpt);
                if (cmRes.Status != PromptStatus.OK && cmRes.Status != PromptStatus.None) return;

                int cm = varsayilan;
                if (cmRes.Status == PromptStatus.OK && !string.IsNullOrEmpty(cmRes.StringResult))
                {
                    if (int.TryParse(cmRes.StringResult, out int parsed))
                        cm = parsed;
                }
                _sonCm = cm; // Hatırla

                PromptPointOptions ppo = new PromptPointOptions("\nBir nokta secin: ");
                PromptPointResult ppr = ed.GetPoint(ppo);

                if (ppr.Status == PromptStatus.OK)
                {
                    Point3d pt = ppr.Value;
                    ed.WriteMessage($"\n  Secilen nokta: X={pt.X:F2}, Y={pt.Y:F2}");

                    KoordinatDonusumServisi.TmToLatLon(
                        pt.X, pt.Y, cm, kuzeyYarikure,
                        out double lat, out double lon);

                    ed.WriteMessage($"\n  WGS84: {lat:F6}, {lon:F6} (CM={cm}°)");

                    callback?.Invoke(lat, lon);
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nHata: {ex.Message}");
            }
        }
    }
}
