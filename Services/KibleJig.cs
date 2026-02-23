using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using KibleYonu.Models;

namespace KibleYonu.Services
{
    /// <summary>
    /// DrawJig — Gerçek zamanlı pusula önizlemesi.
    /// Artık UTM zone yerine enlem/boylam ile çalışır (dünya geneli destek).
    /// </summary>
    public class KibleJig : DrawJig
    {
        private Point3d _basePoint;
        private readonly double _yaricap;
        private PusulaGeometri _geometri;

        // Konum bilgisi (panel tarafından set edilir)
        private readonly double _enlem;
        private readonly double _boylam;
        private readonly bool _sabitKonum;

        // TM/UTM ile çalışma
        private readonly int _merkezMeridyen;
        private readonly bool _kuzeyYarikure;
        private readonly bool _tmModu;

        public PusulaGeometri Geometri => _geometri;

        /// <summary>
        /// Sabit enlem/boylam ile Jig (WPF panelden geliyor).
        /// </summary>
        public KibleJig(double yaricap, double enlem, double boylam)
        {
            _yaricap = yaricap;
            _enlem = enlem;
            _boylam = boylam;
            _sabitKonum = true;
            _tmModu = false;
            _basePoint = Point3d.Origin;
        }

        /// <summary>
        /// TM modu — fare konumundan merkez meridyen ile enlem/boylam hesaplar.
        /// </summary>
        public KibleJig(double yaricap, int merkezMeridyen, bool kuzeyYarikure = true)
        {
            _yaricap = yaricap;
            _merkezMeridyen = merkezMeridyen;
            _kuzeyYarikure = kuzeyYarikure;
            _tmModu = true;
            _sabitKonum = false;
            _basePoint = Point3d.Origin;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions opts = new JigPromptPointOptions(
                "\nKible pusulasini yerlestirmek icin tiklayin: ");
            opts.UserInputControls = UserInputControls.Accept3dCoordinates;

            PromptPointResult ppr = prompts.AcquirePoint(opts);

            if (ppr.Status == PromptStatus.OK)
            {
                if (_basePoint.DistanceTo(ppr.Value) < 0.001)
                    return SamplerStatus.NoChange;

                _basePoint = ppr.Value;

                if (_sabitKonum)
                {
                    _geometri = PusulaGeometri.Hesapla(_basePoint, _yaricap, _enlem, _boylam);
                }
                else if (_tmModu)
                {
                    KoordinatDonusumServisi.TmToLatLon(
                        _basePoint.X, _basePoint.Y, _merkezMeridyen, _kuzeyYarikure,
                        out double lat, out double lon);
                    _geometri = PusulaGeometri.Hesapla(_basePoint, _yaricap, lat, lon);
                }

                return SamplerStatus.OK;
            }

            return SamplerStatus.Cancel;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (_basePoint == Point3d.Origin)
                return true;

            PusulaGeometri geo = _geometri;
            WorldGeometry geom = draw.Geometry;
            Point3d c = geo.Merkez;
            double R = geo.Yaricap;
            double z = c.Z;

            // ── Pusula halkaları ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkHalka;
            geom.Circle(c, R, Vector3d.ZAxis);
            geom.Circle(c, R * PusulaGeometri.IcHalka, Vector3d.ZAxis);
            draw.SubEntityTraits.Color = PusulaGeometri.RenkRefCizgi;
            geom.Circle(c, R * PusulaGeometri.MerkezDaire, Vector3d.ZAxis);

            // ── Derece işaretleri ──
            for (int compass = 0; compass < 360; compass += 10)
            {
                double acad = (90.0 - compass) * Math.PI / 180.0;
                double icR;

                if (compass % 90 == 0)
                {
                    icR = R * PusulaGeometri.TickBuyukIc;
                    draw.SubEntityTraits.Color = PusulaGeometri.RenkTickBuyuk;
                }
                else if (compass % 30 == 0)
                {
                    icR = R * PusulaGeometri.TickOrtaIc;
                    draw.SubEntityTraits.Color = PusulaGeometri.RenkTickBuyuk;
                }
                else
                {
                    icR = R * PusulaGeometri.TickKucukIc;
                    draw.SubEntityTraits.Color = PusulaGeometri.RenkTickKucuk;
                }
                double disR = R * PusulaGeometri.TickDis;

                geom.WorldLine(PusulaGeometri.Polar(c, acad, icR),
                               PusulaGeometri.Polar(c, acad, disR));
            }

            // ── Referans çizgileri ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkRefCizgi;
            double ri = R * PusulaGeometri.RefCizgiIc;
            double ro = R * PusulaGeometri.RefCizgiDis;
            geom.WorldLine(new Point3d(c.X, c.Y + ri, z), new Point3d(c.X, c.Y + ro, z));
            geom.WorldLine(new Point3d(c.X, c.Y - ri, z), new Point3d(c.X, c.Y - ro, z));
            geom.WorldLine(new Point3d(c.X + ri, c.Y, z), new Point3d(c.X + ro, c.Y, z));
            geom.WorldLine(new Point3d(c.X - ri, c.Y, z), new Point3d(c.X - ro, c.Y, z));

            // ── Yön etiketleri ──
            double eR = R * PusulaGeometri.YonEtiket;
            double bTxt = R * PusulaGeometri.BuyukMetin;
            double kTxt = R * PusulaGeometri.KucukMetin;

            draw.SubEntityTraits.Color = PusulaGeometri.RenkKuzey;
            geom.Text(new Point3d(c.X - bTxt * 0.3, c.Y + eR - bTxt * 0.4, z),
                Vector3d.ZAxis, Vector3d.XAxis, bTxt, 1.0, 0.0, "K");

            draw.SubEntityTraits.Color = PusulaGeometri.RenkYon;
            geom.Text(new Point3d(c.X - bTxt * 0.3, c.Y - eR - bTxt * 0.4, z),
                Vector3d.ZAxis, Vector3d.XAxis, bTxt, 1.0, 0.0, "G");
            geom.Text(new Point3d(c.X + eR - bTxt * 0.3, c.Y - bTxt * 0.4, z),
                Vector3d.ZAxis, Vector3d.XAxis, bTxt, 1.0, 0.0, "D");
            geom.Text(new Point3d(c.X - eR - bTxt * 0.3, c.Y - bTxt * 0.4, z),
                Vector3d.ZAxis, Vector3d.XAxis, bTxt, 1.0, 0.0, "B");

            // ── Kuzey oku ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkKuzey;
            double kuzeyUcR = R * PusulaGeometri.KuzeyUcNokta;
            double kuzeyKanatR = R * PusulaGeometri.KuzeyGovdeBitis;
            double kuzeyW = R * 0.045;

            geom.WorldLine(c, new Point3d(c.X, c.Y + kuzeyKanatR, z));

            Point3d nTip = new Point3d(c.X, c.Y + kuzeyUcR, z);
            Point3d nSol = new Point3d(c.X - kuzeyW, c.Y + kuzeyKanatR, z);
            Point3d nSag = new Point3d(c.X + kuzeyW, c.Y + kuzeyKanatR, z);
            Point3dCollection northPts = new Point3dCollection { nTip, nSol, nSag };
            geom.Polygon(northPts);

            // ── Kıble oku ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkKible;
            double kibleAngle = geo.AcadAcisiRad;
            Point3d kbStart = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.OkBaslangic);
            Point3d kbMid = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.KibleGovdeBitis);
            Point3d kbTip = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.KibleUcNokta);

            geom.WorldLine(kbStart, kbMid);

            double headLen = R * 0.14;
            double headAng = 22.0 * Math.PI / 180.0;
            Point3d kbSol = new Point3d(
                kbTip.X - headLen * Math.Cos(kibleAngle - headAng),
                kbTip.Y - headLen * Math.Sin(kibleAngle - headAng), z);
            Point3d kbSag = new Point3d(
                kbTip.X - headLen * Math.Cos(kibleAngle + headAng),
                kbTip.Y - headLen * Math.Sin(kibleAngle + headAng), z);

            Point3dCollection kbPts = new Point3dCollection { kbTip, kbSol, kbSag };
            geom.Polygon(kbPts);

            // ── Açı yayı ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkAciYay;
            Vector3d startVec = new Vector3d(
                Math.Cos(geo.AcadAcisiRad), Math.Sin(geo.AcadAcisiRad), 0);
            double sweep = geo.KibleAcisi * Math.PI / 180.0;
            geom.CircularArc(c, R * PusulaGeometri.AciYayR, Vector3d.ZAxis,
                startVec, sweep, ArcType.ArcSimple);

            // ── Açı ve bilgi yazısı ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkAciYay;
            double arcMid = geo.AcadAcisiRad + sweep / 2.0;
            Point3d aciPos = PusulaGeometri.Polar(c, arcMid, R * PusulaGeometri.AciYayR + R * 0.06);
            geom.Text(aciPos, Vector3d.ZAxis, Vector3d.XAxis,
                kTxt, 1.0, 0.0, $"{geo.KibleAcisi:F1}°");

            // ── Kabe sembolü ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkKabe;
            Point3d kabePos = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.DisEtiketR);
            geom.Text(new Point3d(kabePos.X - kTxt, kabePos.Y, z),
                Vector3d.ZAxis, Vector3d.XAxis, kTxt, 1.0, 0.0, "KABE");

            // ── Bilgi yazısı ──
            draw.SubEntityTraits.Color = PusulaGeometri.RenkKible;
            double infoY = c.Y - R * 1.25;
            double nTxtH = R * PusulaGeometri.NormalMetin;
            geom.Text(new Point3d(c.X - R * 0.4, infoY, z),
                Vector3d.ZAxis, Vector3d.XAxis,
                R * PusulaGeometri.BuyukMetin, 1.0, 0.0, "KIBLE YONU");

            draw.SubEntityTraits.Color = PusulaGeometri.RenkYon;
            geom.Text(new Point3d(c.X - R * 0.5, infoY - nTxtH * 2.5, z),
                Vector3d.ZAxis, Vector3d.XAxis,
                nTxtH * 0.7, 1.0, 0.0, geo.Etiket);

            return true;
        }
    }
}
