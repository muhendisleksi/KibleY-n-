using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace KibleYonu.Commands
{
    public class KibleCommands
    {
        /// <summary>
        /// Ana komut — Kıble Yönü panelini aç/kapat.
        /// </summary>
        [CommandMethod("KIBLEYONU")]
        public void KibleYonuPanel()
        {
            UIManager.PanelToggle();
        }

        /// <summary>
        /// Panel kapat komutu.
        /// </summary>
        [CommandMethod("KIBLEYONU_KAPAT")]
        public void KibleKapat()
        {
            UIManager.PanelKapat();
        }

        /// <summary>
        /// Hızlı CLI modu — panel olmadan doğrudan UTM ile hesaplama (geriye uyumluluk).
        /// </summary>
        [CommandMethod("KIBLEYONU_HIZLI")]
        public void KibleHizli()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // UTM Zone
                PromptIntegerOptions zoneOpt = new PromptIntegerOptions("\nUTM Zone [1-60]: ");
                zoneOpt.DefaultValue = 36;
                zoneOpt.LowerLimit = 1;
                zoneOpt.UpperLimit = 60;
                PromptIntegerResult zoneRes = ed.GetInteger(zoneOpt);
                if (zoneRes.Status != PromptStatus.OK) return;
                int zone = zoneRes.Value;

                // Yarıçap
                PromptDoubleOptions radOpt = new PromptDoubleOptions("\nPusula yaricapi: ");
                radOpt.DefaultValue = 100.0;
                PromptDoubleResult radRes = ed.GetDouble(radOpt);
                if (radRes.Status != PromptStatus.OK) return;
                double yaricap = radRes.Value;

                ed.WriteMessage("\nFareyi hareket ettirin, tiklayarak yerlestirin...\n");

                var jig = new Services.KibleJig(yaricap, zone);
                PromptResult pr = ed.Drag(jig);

                if (pr.Status == PromptStatus.OK)
                {
                    var geo = jig.Geometri;
                    var cizimServisi = new Services.PusulaCizimServisi();
                    cizimServisi.PusulaCiz(doc.Database, geo, Models.DetaySeviyesi.Normal, true);

                    ed.WriteMessage($"\n========================================");
                    ed.WriteMessage($"\n  KIBLE YONU");
                    ed.WriteMessage($"\n========================================");
                    ed.WriteMessage($"\n  Koordinat : {geo.Enlem:F4}°N, {geo.Boylam:F4}°E");
                    ed.WriteMessage($"\n  Aci       : {geo.KibleAcisi:F1}°");
                    ed.WriteMessage($"\n  Mesafe    : {geo.UzaklikKm:F0} km");
                    ed.WriteMessage($"\n========================================\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nHata: {ex.Message}");
            }
        }
    }
}
