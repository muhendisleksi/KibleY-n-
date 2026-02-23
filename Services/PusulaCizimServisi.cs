using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using KibleYonu.Models;

namespace KibleYonu.Services
{
    public class PusulaCizimServisi
    {
        /// <summary>
        /// Veritabanına pusula çizer ve tüm entity'leri gruplar.
        /// </summary>
        public void PusulaCiz(Database db, PusulaGeometri geo, DetaySeviyesi detay, bool bilgiPaneli)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                ObjectIdCollection gIds = new ObjectIdCollection();

                Point3d c = geo.Merkez;
                double R = geo.Yaricap;
                double z = c.Z;

                // ━━ 1) PUSULA HALKALARI ━━
                Circle disHalka = new Circle(c, Vector3d.ZAxis, R);
                disHalka.ColorIndex = PusulaGeometri.RenkHalka;
                disHalka.LineWeight = LineWeight.LineWeight050;
                Add(btr, tr, disHalka, gIds);

                Circle icHalka = new Circle(c, Vector3d.ZAxis, R * PusulaGeometri.IcHalka);
                icHalka.ColorIndex = PusulaGeometri.RenkTickKucuk;
                icHalka.LineWeight = LineWeight.LineWeight015;
                Add(btr, tr, icHalka, gIds);

                if (detay != DetaySeviyesi.Basit)
                {
                    Circle merkezD = new Circle(c, Vector3d.ZAxis, R * PusulaGeometri.MerkezDaire);
                    merkezD.ColorIndex = PusulaGeometri.RenkRefCizgi;
                    merkezD.LineWeight = LineWeight.LineWeight009;
                    Add(btr, tr, merkezD, gIds);

                    Circle merkezN = new Circle(c, Vector3d.ZAxis, R * PusulaGeometri.MerkezNokta);
                    merkezN.ColorIndex = PusulaGeometri.RenkHalka;
                    Add(btr, tr, merkezN, gIds);
                }

                // ━━ 2) DERECE İŞARETLERİ ━━
                int adim = detay == DetaySeviyesi.Basit ? 30 : 10;
                for (int compass = 0; compass < 360; compass += adim)
                {
                    double acad = (90.0 - compass) * Math.PI / 180.0;
                    double icR, disR;
                    short renk;
                    LineWeight lw;

                    if (compass % 90 == 0)
                    {
                        icR = R * PusulaGeometri.TickBuyukIc;
                        disR = R * PusulaGeometri.TickDis;
                        renk = PusulaGeometri.RenkTickBuyuk;
                        lw = LineWeight.LineWeight035;
                    }
                    else if (compass % 30 == 0)
                    {
                        icR = R * PusulaGeometri.TickOrtaIc;
                        disR = R * PusulaGeometri.TickDis;
                        renk = PusulaGeometri.RenkTickBuyuk;
                        lw = LineWeight.LineWeight020;
                    }
                    else
                    {
                        icR = R * PusulaGeometri.TickKucukIc;
                        disR = R * PusulaGeometri.TickDis;
                        renk = PusulaGeometri.RenkTickKucuk;
                        lw = LineWeight.LineWeight013;
                    }

                    Point3d p1 = PusulaGeometri.Polar(c, acad, icR);
                    Point3d p2 = PusulaGeometri.Polar(c, acad, disR);

                    Line tick = new Line(p1, p2);
                    tick.ColorIndex = renk;
                    tick.LineWeight = lw;
                    Add(btr, tr, tick, gIds);
                }

                // ━━ 3) REFERANS ÇİZGİLERİ ━━
                if (detay != DetaySeviyesi.Basit)
                {
                    double refIc = R * PusulaGeometri.RefCizgiIc;
                    double refDis = R * PusulaGeometri.RefCizgiDis;

                    AddRefLine(btr, tr, gIds, c, new Point3d(c.X, c.Y + refIc, z), new Point3d(c.X, c.Y + refDis, z));
                    AddRefLine(btr, tr, gIds, c, new Point3d(c.X, c.Y - refIc, z), new Point3d(c.X, c.Y - refDis, z));
                    AddRefLine(btr, tr, gIds, c, new Point3d(c.X + refIc, c.Y, z), new Point3d(c.X + refDis, c.Y, z));
                    AddRefLine(btr, tr, gIds, c, new Point3d(c.X - refIc, c.Y, z), new Point3d(c.X - refDis, c.Y, z));
                }

                // ━━ 4) YÖN ETİKETLERİ ━━
                double eR = R * PusulaGeometri.YonEtiket;
                double bTxt = R * PusulaGeometri.BuyukMetin;
                double kTxt = R * PusulaGeometri.KucukMetin;

                AddText(btr, tr, gIds, "K", new Point3d(c.X, c.Y + eR, z), bTxt, PusulaGeometri.RenkKuzey);
                AddText(btr, tr, gIds, "G", new Point3d(c.X, c.Y - eR, z), bTxt, PusulaGeometri.RenkYon);
                AddText(btr, tr, gIds, "D", new Point3d(c.X + eR, c.Y, z), bTxt, PusulaGeometri.RenkYon);
                AddText(btr, tr, gIds, "B", new Point3d(c.X - eR, c.Y, z), bTxt, PusulaGeometri.RenkYon);

                if (detay == DetaySeviyesi.Detayli)
                {
                    double araR = R * 0.80;
                    double cos45 = Math.Cos(Math.PI / 4);
                    double sin45 = Math.Sin(Math.PI / 4);

                    AddText(btr, tr, gIds, "KD", new Point3d(c.X + araR * cos45, c.Y + araR * sin45, z), kTxt, PusulaGeometri.RenkTickKucuk);
                    AddText(btr, tr, gIds, "KB", new Point3d(c.X - araR * cos45, c.Y + araR * sin45, z), kTxt, PusulaGeometri.RenkTickKucuk);
                    AddText(btr, tr, gIds, "GD", new Point3d(c.X + araR * cos45, c.Y - araR * sin45, z), kTxt, PusulaGeometri.RenkTickKucuk);
                    AddText(btr, tr, gIds, "GB", new Point3d(c.X - araR * cos45, c.Y - araR * sin45, z), kTxt, PusulaGeometri.RenkTickKucuk);
                }

                // ━━ 5) KUZEY OKU ━━
                double kuzeyUcR = R * PusulaGeometri.KuzeyUcNokta;
                double kuzeyKanatR = R * PusulaGeometri.KuzeyGovdeBitis;
                double kuzeyW = R * 0.045;

                Line kuzeyGovde = new Line(c, new Point3d(c.X, c.Y + kuzeyKanatR, z));
                kuzeyGovde.ColorIndex = PusulaGeometri.RenkKuzey;
                kuzeyGovde.LineWeight = LineWeight.LineWeight030;
                Add(btr, tr, kuzeyGovde, gIds);

                Point3d kTip = new Point3d(c.X, c.Y + kuzeyUcR, z);
                Point3d kSol = new Point3d(c.X - kuzeyW, c.Y + kuzeyKanatR, z);
                Point3d kSag = new Point3d(c.X + kuzeyW, c.Y + kuzeyKanatR, z);
                Solid kuzeyOkBasi = new Solid(kTip, kSol, kSag, kSag);
                kuzeyOkBasi.ColorIndex = PusulaGeometri.RenkKuzey;
                Add(btr, tr, kuzeyOkBasi, gIds);

                // ━━ 6) KIBLE OKU ━━
                double kibleAngle = geo.AcadAcisiRad;
                Point3d kibleStart = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.OkBaslangic);
                Point3d kibleMid = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.KibleGovdeBitis);
                Point3d kibleTip = PusulaGeometri.Polar(c, kibleAngle, R * PusulaGeometri.KibleUcNokta);

                Line kibleGovde = new Line(kibleStart, kibleMid);
                kibleGovde.ColorIndex = PusulaGeometri.RenkKible;
                kibleGovde.LineWeight = LineWeight.LineWeight050;
                Add(btr, tr, kibleGovde, gIds);

                double headLen = R * 0.14;
                double headAng = 22.0 * Math.PI / 180.0;
                Point3d kbSol = new Point3d(
                    kibleTip.X - headLen * Math.Cos(kibleAngle - headAng),
                    kibleTip.Y - headLen * Math.Sin(kibleAngle - headAng), z);
                Point3d kbSag = new Point3d(
                    kibleTip.X - headLen * Math.Cos(kibleAngle + headAng),
                    kibleTip.Y - headLen * Math.Sin(kibleAngle + headAng), z);
                Solid kibleOkBasi = new Solid(kibleTip, kbSol, kbSag, kbSag);
                kibleOkBasi.ColorIndex = PusulaGeometri.RenkKible;
                Add(btr, tr, kibleOkBasi, gIds);

                // ━━ 7) AÇI YAYI ━━
                double yayR = R * PusulaGeometri.AciYayR;
                double kibleAcadDeg = geo.AcadAcisiRad * 180.0 / Math.PI;

                double arcStart, arcEnd;
                if (kibleAcadDeg < 90.0)
                {
                    arcStart = geo.AcadAcisiRad;
                    arcEnd = Math.PI / 2.0;
                }
                else
                {
                    arcStart = Math.PI / 2.0;
                    arcEnd = geo.AcadAcisiRad;
                }

                while (arcStart < 0) arcStart += 2 * Math.PI;
                while (arcEnd < 0) arcEnd += 2 * Math.PI;

                Arc aciYayi = new Arc(c, yayR, arcStart, arcEnd);
                aciYayi.ColorIndex = PusulaGeometri.RenkAciYay;
                aciYayi.LineWeight = LineWeight.LineWeight025;
                Add(btr, tr, aciYayi, gIds);

                double arcMidAng = (arcStart + arcEnd) / 2.0;
                if (arcEnd < arcStart) arcMidAng += Math.PI;
                Point3d aciTxtPos = PusulaGeometri.Polar(c, arcMidAng, yayR + R * 0.06);
                AddText(btr, tr, gIds, $"{geo.KibleAcisi:F1}°", aciTxtPos, R * PusulaGeometri.KucukMetin, PusulaGeometri.RenkAciYay);

                if (detay != DetaySeviyesi.Basit)
                {
                    double tickLen = R * 0.03;
                    AddTickPair(btr, tr, gIds, c, arcStart, yayR, tickLen);
                    AddTickPair(btr, tr, gIds, c, arcEnd, yayR, tickLen);
                }

                // ━━ 8) KABE SEMBOLÜ ━━
                double kabeR = R * PusulaGeometri.DisEtiketR;
                Point3d kabePos = PusulaGeometri.Polar(c, kibleAngle, kabeR);

                double d = R * 0.025;
                Solid kabeKare = new Solid(
                    new Point3d(kabePos.X - d, kabePos.Y - d, z),
                    new Point3d(kabePos.X + d, kabePos.Y - d, z),
                    new Point3d(kabePos.X - d, kabePos.Y + d, z),
                    new Point3d(kabePos.X + d, kabePos.Y + d, z));
                kabeKare.ColorIndex = PusulaGeometri.RenkKabe;
                Add(btr, tr, kabeKare, gIds);

                Point3d kabeTxtPos = PusulaGeometri.Polar(c, kibleAngle, kabeR + R * 0.06);
                AddText(btr, tr, gIds, "KABE", kabeTxtPos, R * PusulaGeometri.KucukMetin, PusulaGeometri.RenkKabe);

                // ━━ 9) BİLGİ PANELİ ━━
                if (bilgiPaneli)
                {
                    double panelY = c.Y - R * 1.25;
                    double nTxt = R * PusulaGeometri.NormalMetin;

                    AddText(btr, tr, gIds, "KIBLE YONU",
                        new Point3d(c.X, panelY, z), bTxt, PusulaGeometri.RenkKible);

                    double sepW = R * 0.55;
                    Line sep = new Line(
                        new Point3d(c.X - sepW, panelY - bTxt * 1.2, z),
                        new Point3d(c.X + sepW, panelY - bTxt * 1.2, z));
                    sep.ColorIndex = PusulaGeometri.RenkRefCizgi;
                    sep.LineWeight = LineWeight.LineWeight013;
                    Add(btr, tr, sep, gIds);

                    double satir = panelY - bTxt * 2.2;
                    double satirAraligi = nTxt * 1.8;

                    if (!string.IsNullOrEmpty(geo.KonumAdi))
                    {
                        AddText(btr, tr, gIds, geo.KonumAdi,
                            new Point3d(c.X, satir, z), nTxt * 0.9, PusulaGeometri.RenkBilgi);
                        satir -= satirAraligi;
                    }

                    AddText(btr, tr, gIds, $"Koordinat: {geo.Enlem:F4}°N  {geo.Boylam:F4}°E",
                        new Point3d(c.X, satir, z), nTxt * 0.8, PusulaGeometri.RenkYon);
                    satir -= satirAraligi;

                    AddText(btr, tr, gIds, $"Kible Acisi: {geo.KibleAcisi:F1}°",
                        new Point3d(c.X, satir, z), nTxt * 0.8, PusulaGeometri.RenkYon);
                    satir -= satirAraligi;

                    AddText(btr, tr, gIds, $"Kabe Mesafesi: {geo.UzaklikKm:F0} km",
                        new Point3d(c.X, satir, z), nTxt * 0.8, PusulaGeometri.RenkYon);
                }

                // ━━ 10) GRUPLAMA ━━
                DBDictionary groupDict = tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite) as DBDictionary;
                Group group = new Group("Kible Yonu - Modern Pusula", true);
                groupDict.SetAt($"KibleYonu_{DateTime.Now.Ticks}", group);
                tr.AddNewlyCreatedDBObject(group, true);
                foreach (ObjectId id in gIds)
                    group.Append(id);

                tr.Commit();
            }
        }

        private void Add(BlockTableRecord btr, Transaction tr, Entity ent, ObjectIdCollection ids)
        {
            btr.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);
            ids.Add(ent.ObjectId);
        }

        private void AddText(BlockTableRecord btr, Transaction tr, ObjectIdCollection ids,
            string text, Point3d pos, double height, short color)
        {
            MText mt = new MText();
            mt.Location = pos;
            mt.TextHeight = height;
            mt.Contents = text;
            mt.ColorIndex = color;
            mt.Attachment = AttachmentPoint.MiddleCenter;
            Add(btr, tr, mt, ids);
        }

        private void AddRefLine(BlockTableRecord btr, Transaction tr, ObjectIdCollection ids,
            Point3d c, Point3d p1, Point3d p2)
        {
            Line line = new Line(p1, p2);
            line.ColorIndex = PusulaGeometri.RenkRefCizgi;
            line.LineWeight = LineWeight.LineWeight005;
            Add(btr, tr, line, ids);
        }

        private void AddTickPair(BlockTableRecord btr, Transaction tr, ObjectIdCollection ids,
            Point3d c, double angle, double yayR, double tickLen)
        {
            Point3d a = PusulaGeometri.Polar(c, angle, yayR - tickLen);
            Point3d b = PusulaGeometri.Polar(c, angle, yayR + tickLen);
            Line tick = new Line(a, b);
            tick.ColorIndex = PusulaGeometri.RenkAciYay;
            Add(btr, tr, tick, ids);
        }
    }
}
