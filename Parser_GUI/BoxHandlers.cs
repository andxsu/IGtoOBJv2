using Newtonsoft.Json.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Globalization;
using Parser_GUI;
using System;

namespace Parser_GUI
{
    internal class IGBoxes
    {
        private readonly string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private string eventTitle;
        public string jsonData;
        private JObject data;
        // Scaling factors are used to make sure the calorimetry towers and boxes are generated correctly. I don't know why this is. It's just a thing with the way event data is stored. For more info
        // you'd need to talk to the guy behind iSpy and IG files generally. Electromagnetic calorimetry scales are hard coded for some reason
        private double EESCALE;
        private double EBSCALE;
        private double ESSCALE;

        private double HBSCALE;
        private double HESCALE;
        private double HFSCALE;
        private double HOSCALE;

        public List<CalorimetryData> EEData;
        public List<CalorimetryData> EBData;
        public List<CalorimetryData> ESData;

        public List<CalorimetryData> HEData;
        public List<CalorimetryData> HBData;
        public List<CalorimetryData> HFData;
        public List<CalorimetryData> HOData;

        public List<JetData> jetDatas;
        public List<Vertex> vertexDatas;
        public List<MuonChamberData> muonChamberDatas;
        public List<SuperCluster> superClusters;
        public List<List<RecHitFraction>> recHitFractions;
        public IGBoxes(JObject dataFile, string name)
        {
            HBSCALE = 1.0;
            HESCALE = 1.0;
            HFSCALE = 1.0;
            HOSCALE = 1.0;
            data = dataFile;
            eventTitle = name;
            if (!Directory.Exists($"{eventTitle}"))
            {
                Directory.CreateDirectory($"{eventTitle}");
            }
            setScales();
            Execute();
        }
        public void Execute()
        {
            makeEBRec();
            makeEERec();
            makeESRec();
            makeHBRec();
            makeHERec();
            makeHFRec();
            makeHORec();
            muonChamberDatas = muonChamberParse();
            generateMuonChamberModels(muonChamberDatas);
            List<JetData> jetList = jetParse();
            generateJetModels(jetList);
            superClusters = superClusterParse();
            var yuh = recHitFractionsParse();
            recHitFractions = assignRecHitFractions(yuh);
            makeSuperClusters();
            try
            {
                vertexDatas = vertexParse();
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error: {ex.Message}");
            }
            int i = 0;
            try
            {
                foreach (Vertex v in vertexDatas)
                {
                    GenerateEllipsoidObj($@"{eventTitle}\vertices.obj", vertexDatas, 3.0);
                    i += 1;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error: {ex.Message}");
            }

        }
        public List<MuonChamberData> muonChamberParse()
        {
            /* 
             * UGH, I have to work on a way to get the canvases in unity aligned properly with the muon chambers so that we can display 
             * detector ID. I want to try changing the way the muon chamber data gets parsed out here. The problem is that the CSCs
             * narrow as they move inwards, so I need to set the vertical/horizontal component to only choose the smaller one.
             * Doing that will make sure that the canvasses don't extend past the models and the detector ID can be shown 
             * directly on the chamber. Add a ternary operator that chooses the centroid vector based on whether back1 or front1 is closer to origin
            */
            var dataList = new List<MuonChamberData>();
            var vectorlist = new List<string>();
            int idNumber = 0;
            var V = Vector<double>.Build;
            if (data["Collections"]["MuonChambers_V1"] != null)
            {
                foreach (var igChamberData in data["Collections"]["MuonChambers_V1"])
                {
                    if (vectorlist.Contains(igChamberData[1].ToString()))
                    {
                        continue;
                    }
                    else
                    {
                        vectorlist.Add(igChamberData[1].ToString());
                    }
                    MuonChamberData muonChamberData = new MuonChamberData();
                    var children = igChamberData.Children().Values<double>().ToArray();

                    muonChamberData.name = "MuonChambers_V1";
                    muonChamberData.id = idNumber;
                    muonChamberData.detid = (int)children[0];
                    muonChamberData.front_1 = new double[] { children[1], children[2], children[3] };
                    muonChamberData.front_2 = new double[] { children[4], children[5], children[6] };
                    muonChamberData.front_3 = new double[] { children[7], children[8], children[9] };
                    muonChamberData.front_4 = new double[] { children[10], children[11], children[12] };
                    muonChamberData.back_1 = new double[] { children[13], children[14], children[15] };
                    muonChamberData.back_2 = new double[] { children[16], children[17], children[18] };
                    muonChamberData.back_3 = new double[] { children[19], children[20], children[21] };
                    muonChamberData.back_4 = new double[] { children[22], children[23], children[24] };
                    muonChamberData.vertical = new double[] { children[1] - children[10], children[2] - children[11], children[3] - children[12] };
                    muonChamberData.horizontal = new double[] { children[1] - children[4], children[2] - children[5], children[3] - children[6] };

                    var front1 = V.DenseOfArray(muonChamberData.front_1);
                    var back1 = V.DenseOfArray(muonChamberData.back_1);

                    if (front1.L2Norm() < back1.L2Norm())
                    {
                        muonChamberData.centroid = new double[] { (children[1] + children[4] + children[7] + children[10]) / 4.0, (children[2] + children[5] + children[8] + children[11])/4.0,
                        (children[3]+children[6]+children[9]+children[12])/4.0};
                    }
                    else
                    {
                        muonChamberData.centroid = new double[] { (children[13] + children[16] + children[19] + children[22]) / 4.0, (children[14] + children[17] + children[20] + children[23])/4.0,
                    (children[15]+children[18]+children[21]+children[24])/4.0};
                    }


                    idNumber++;

                    dataList.Add(muonChamberData);
                }
            }
            return dataList;
        }
        public void generateMuonChamberModels(List<MuonChamberData> data)
        {
            if (data.Count() == 0) { return; }

            List<string> dataStrings = new List<string>();
            List<string> normals = new List<string>();
            int index = 0;
            int counter = 1;
            //dataStrings.Add("vn -1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 -1.0000\nvn 1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 1.0000\nvn -0.0000 -1.0000 -0.0000\nvn -0.0000 1.0000 -0.0000");

            foreach (var chamber in data)
            {
                dataStrings.Add($"o MuonChamber_{index}");
                dataStrings.Add($"v {String.Join(' ', chamber.front_1)}");
                dataStrings.Add($"v {String.Join(' ', chamber.front_2)}");
                dataStrings.Add($"v {String.Join(' ', chamber.front_3)}");
                dataStrings.Add($"v {String.Join(' ', chamber.front_4)}");
                dataStrings.Add($"v {String.Join(' ', chamber.back_1)}");
                dataStrings.Add($"v {String.Join(' ', chamber.back_2)}");
                dataStrings.Add($"v {String.Join(' ', chamber.back_3)}");
                dataStrings.Add($"v {String.Join(' ', chamber.back_4)}");
                Vector3D f1_1 = (new Vector3D(chamber.front_1[0], chamber.front_1[1], chamber.front_1[2])) - (new Vector3D(chamber.front_2[0], chamber.front_2[1], chamber.front_2[2]));
                Vector3D f1_2 = (new Vector3D(chamber.front_1[0], chamber.front_1[1], chamber.front_1[2])) - (new Vector3D(chamber.front_3[0], chamber.front_3[1], chamber.front_3[2]));
                Vector3D normal2_1 = (new Vector3D(chamber.front_1[0], chamber.front_1[1], chamber.front_1[2])) - (new Vector3D(chamber.back_1[0], chamber.back_1[1], chamber.back_1[2]));
                Vector3D normal2_2 = (new Vector3D(chamber.back_1[0], chamber.back_1[1], chamber.back_1[2])) - (new Vector3D(chamber.back_2[0], chamber.back_2[1], chamber.back_2[2]));
                Vector3D normal;
                Vector3D norm1 = f1_1.CrossProduct(f1_2);

                dataStrings.Add($"f {counter + 3} {counter + 2} {counter + 1} {counter}");
                dataStrings.Add($"f {counter + 4} {counter + 5} {counter + 6} {counter + 7}");
                dataStrings.Add($"f {counter + 1} {counter + 2} {counter + 6} {counter + 5}");
                dataStrings.Add($"f {counter + 4} {counter + 7} {counter + 3} {counter}");
                dataStrings.Add($"f {counter + 2} {counter + 3} {counter + 7} {counter + 6}");
                dataStrings.Add($"f {counter} {counter + 1} {counter + 5} {counter + 4}");
                index++;
                counter += 8;
            }
            File.WriteAllText($"{eventTitle}\\7_MuonChambers_V1.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\7_MuonChambers_V1.obj", dataStrings);
        }
        public List<CalorimetryData> genericCaloParse(string name, double scale)
        {
            List<CalorimetryData> dataList = new List<CalorimetryData>();

            foreach (var item in data["Collections"][name])
            {
                CalorimetryData caloItem = new CalorimetryData();
                var children = item.Children().Values<double>().ToArray();

                caloItem.energy = children[0];
                caloItem.scale = caloItem.energy / scale;
                caloItem.eta = children[1];
                caloItem.phi = children[2];
                caloItem.time = children[3];
                caloItem.detid = (int)children[4];
                caloItem.front_1 = new double[] { children[5], children[6], children[7] };
                caloItem.front_2 = new double[] { children[8], children[9], children[10] };
                caloItem.front_3 = new double[] { children[11], children[12], children[13] };
                caloItem.front_4 = new double[] { children[14], children[15], children[16] };
                caloItem.back_1 = new double[] { children[17], children[18], children[19] };
                caloItem.back_2 = new double[] { children[20], children[21], children[22] };
                caloItem.back_3 = new double[] { children[23], children[24], children[25] };
                caloItem.back_4 = new double[] { children[26], children[27], children[28] };
                dataList.Add(caloItem);
            }

            return dataList;
        }
        public void makeHFRec()
        {
            HFData = genericCaloParse("HFRecHits_V2", HFSCALE);
            if (HFData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\6_HFRecHits_V2.obj", String.Empty);
                return;
            }
            List<string> dataList = generateCalorimetryBoxes(HFData);
            File.WriteAllText($"{eventTitle}\\6_HFRecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\6_HFRecHits_V2.obj", dataList);
        }
        public void makeHBRec()
        {
            HBData = genericCaloParse("HBRecHits_V2", HBSCALE);
            List<string> dataList = generateCalorimetryBoxes(HBData);
            if (HBData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\6_HBRecHits_V2.obj", String.Empty);
                return;
            }
            File.WriteAllText($"{eventTitle}\\6_HBRecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\6_HBRecHits_V2.obj", dataList);
        }
        public void makeHERec()
        {
            HEData = genericCaloParse("HERecHits_V2", HESCALE);
            List<string> dataList = generateCalorimetryBoxes(HEData);
            if (HEData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\6_HERecHits_V2.obj", String.Empty);
                return;
            }
            File.WriteAllText($"{eventTitle}\\6_HERecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\6_HERecHits_V2.obj", dataList);
        }
        public void makeHORec()
        {
            HOData = genericCaloParse("HORecHits_V2", HOSCALE);
            List<string> dataList = generateCalorimetryTowers(HOData);
            if (HOData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\6_HORecHits_V2.obj", String.Empty);

                return;
            }
            File.WriteAllText($"{eventTitle}\\6_HORecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\6_HORecHits_V2.obj", dataList);
        }
        public void makeEBRec()
        {
            EBData = genericCaloParse("EBRecHits_V2", EBSCALE);
            List<string> dataList = generateCalorimetryTowers(EBData);
            if (EBData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\5_EBRecHits_V2.obj", String.Empty);
                return;
            }
            File.WriteAllText($"{eventTitle}\\5_EBRecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\5_EBRecHits_V2.obj", dataList);
        }
        public void makeEERec()
        {
            EEData = genericCaloParse("EERecHits_V2", EESCALE);
            List<string> dataList = generateCalorimetryTowers(EEData);
            if (EEData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\5_EERecHits_V2.obj", String.Empty);
                return;
            }
            File.WriteAllText($"{eventTitle}\\5_EERecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\5_EERecHits_V2.obj", dataList);
        }
        public void makeESRec()
        {
            ESData = genericCaloParse("ESRecHits_V2", ESSCALE);
            List<string> dataList = generateCalorimetryTowers(ESData);
            if (ESData.Count == 0)
            {
                File.WriteAllText($"{eventTitle}\\5_ESRecHits_V2.obj", String.Empty);

                return;
            }
            File.WriteAllText($"{eventTitle}\\5_ESRecHits_V2.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\5_ESRecHits_V2.obj", dataList);
        }
        public List<JetData> jetParse()
        {
            int idNumber = 0;
            List<JetData> datalist = new List<JetData>();
            foreach (var item in data["Collections"]["PFJets_V1"])
            {

                JetData currentJet = new JetData();
                var children = item.Children().Values<double>().ToArray();

                currentJet.id = idNumber;
                currentJet.et = children[0];
                currentJet.eta = children[1];
                currentJet.theta = children[2];
                currentJet.phi = children[3];

                idNumber++;
                datalist.Add(currentJet);
            }
            jetDatas = datalist;

            return datalist;
        }
        public void generateJetModels(List<JetData> data)
        {
            double maxZ = 2.25;
            double maxR = 1.10;
            double radius = 0.3 * (1.0 / (1 + 0.001));
            int numSections = 64;
            int iterNumber = 0;
            int index = 0;
            List<string> dataList = new List<string>();

            foreach (var item in data)
            {
                iterNumber++;
                double ct = Math.Cos(item.theta);
                double st = Math.Sin(item.theta);

                double length1 = (ct != 0.0) ? maxZ / Math.Abs(ct) : maxZ;
                double length2 = (st != 0.0) ? maxR / Math.Abs(st) : maxR;
                double length = length1 < length2 ? length1 : length2;

                dataList = jetGeometry(item, radius, length, numSections, index, dataList);
                index++;
            }
            File.WriteAllLines($"{eventTitle}//0_PFJets.obj", dataList);
        }
        public List<string> jetGeometry(JetData item, double radius, double length, int sections, int index, List<string> dataList)
        {
            List<string> normals = new List<string>();
            List<string> normals1 = new List<string>();
            List<string> normals2 = new List<string>();
            List<string> section1 = new List<string>();
            List<string> topsection = new List<string>();
            List<Vector3D> radialpoints = new List<Vector3D>();
            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            double[,] xRot =
                { { 1, 0, 0 },
                { 0, Math.Cos(item.theta), -1.0 * Math.Sin(item.theta) },
                { 0, Math.Sin(item.theta), Math.Cos(item.theta) } };

            double[,] zRot =
                { { Math.Cos(item.phi+Math.PI/2.0), -1.0 * Math.Sin(item.phi+Math.PI/2.0), 0 },
                { Math.Sin(item.phi+Math.PI/2.0), Math.Cos(item.phi+Math.PI/2.0), 0 },
                { 0, 0, 1 } };

            var rx = M.DenseOfArray(xRot); //Rotation matrices
            var rz = M.DenseOfArray(zRot);
            normals.Add($"o Jets_{index}");


            for (double i = 1.0; i <= sections; i++)
            {
                double radian = (2.0 * i * Math.PI) / (double)sections;

                string bottompoint = "v 0 0 0";
                section1.Add(bottompoint);

                double[] feederArray = { radius * Math.Cos(radian), radius * Math.Sin(radian), length };
                Vector<double> temptop = Vector<double>.Build.DenseOfArray(feederArray);

                var rotation = rz * rx;
                var top = rotation * temptop;

                //We can use the toppoint list as the vector list to generate normals with. Make a new for loop to handle this
                string toppoint = $"v {top[0]} {top[1]} {top[2]}";
                topsection.Add(toppoint);
                radialpoints.Add(new Vector3D(top[0], top[1], top[2]));
            }
            section1.AddRange(topsection);
            for (int i = 0; i < radialpoints.Count; i++)
            {
                if (i == radialpoints.Count - 1)
                {
                    var vector_1 = radialpoints[i];
                    var vector_2 = radialpoints[0];
                    Vector3D norm = vector_1.CrossProduct(vector_2);
                    normals.Add($"vn {norm.X} {norm.Y} {norm.Z}");
                    normals2.Add($"vn {-norm.X} {-norm.Y} {-norm.Z}");
                    break;
                }
                var vector1 = radialpoints[i];
                var vector2 = radialpoints[i + 1];

                Vector3D normalresult = vector1.CrossProduct(vector2);
                normals.Add($"vn {normalresult.X} {normalresult.Y} {normalresult.Z}");
                normals2.Add($"vn {-normalresult.X} {-normalresult.Y} {-normalresult.Z}");
            }
            normals.AddRange(normals2);
            int n = 1;

            while (n < sections)
            {
                string face = $"f {n + (2 * sections * index)}//{n + (2 * sections * index)} {n + sections + (2 * sections * index)}//{n + (2 * sections * index)} {n + 1 + sections + (2 * sections * index)}//{n + (2 * sections * index)} {n + 1 + (2 * sections * index)}//{n + (2 * sections * index)}";
                //string face = $"f {n} {n + sections} {n + 1 + sections} {n + 1}";
                string revface = $"f {n + 1 + (2 * sections * index)}//{n + sections + (2 * sections * index)} {n + 1 + sections + (2 * sections * index)}//{n + sections + (2 * sections * index)} {n + sections + (2 * sections * index)}//{n + sections + (2 * sections * index)} {n + (2 * sections * index)}//{n + sections + (2 * sections * index)}";
                section1.Add(face);
                section1.Add(revface);
                n++;
            }

            section1.Add($"f {2 * sections * index + sections}//{2 * sections * index + sections} {2 * sections * index + 2 * sections}//{2 * sections * index + sections} {2 * sections * index + sections + 1}//{2 * sections * index + sections} {2 * sections * index + 1}//{2 * sections * index + sections}\n" +
                $"f {2 * sections * index + 1}//{2 * sections * index + 2 * sections} {2 * sections * index + sections + 1}//{2 * sections * index + 2 * sections} {2 * sections * index + 2 * sections}//{2 * sections * index + 2 * sections} {2 * sections * index + sections}//{2 * sections * index + 2 * sections}");
            normals.AddRange(section1);
            dataList.AddRange(normals);
            return dataList;
        }
        public List<string> generateCalorimetryBoxes(List<CalorimetryData> inputData)
        {
            List<string> geometryData = new List<string>();
            int counter = 1;

            geometryData.Add("vn -1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 -1.0000\nvn 1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 1.0000\nvn -0.0000 -1.0000 -0.0000\nvn -0.0000 1.0000 -0.0000");

            var V = Vector<double>.Build;

            foreach (CalorimetryData box in inputData)
            {
                double scale = box.scale;

                var v0 = V.DenseOfArray(box.front_1);
                var v1 = V.DenseOfArray(box.front_2);
                var v2 = V.DenseOfArray(box.front_3);
                var v3 = V.DenseOfArray(box.front_4);
                var v4 = V.DenseOfArray(box.back_1);
                var v5 = V.DenseOfArray(box.back_2);
                var v6 = V.DenseOfArray(box.back_3);
                var v7 = V.DenseOfArray(box.back_4);

                var center = v0 + v1;
                center += v2;
                center += v3;
                center += v4;
                center += v5;
                center += v6;
                center += v7;
                center /= 8.0;

                v0 -= center;
                v0 *= scale;
                v0 += center;
                v1 -= center;
                v1 *= scale;
                v1 += center;
                v2 -= center;
                v2 *= scale;
                v2 += center;
                v3 -= center;
                v3 *= scale;
                v3 += center;
                v4 -= center;
                v4 *= scale;
                v4 += center;
                v5 -= center;
                v5 *= scale;
                v5 += center;
                v6 -= center;
                v6 *= scale;
                v6 += center;
                v7 -= center;
                v7 *= scale;
                v7 += center;

                geometryData.Add($"v {String.Join(' ', v0)}");
                geometryData.Add($"v {String.Join(' ', v1)}");
                geometryData.Add($"v {String.Join(' ', v2)}");
                geometryData.Add($"v {String.Join(' ', v3)}");
                geometryData.Add($"v {String.Join(' ', v4)}");
                geometryData.Add($"v {String.Join(' ', v5)}");
                geometryData.Add($"v {String.Join(' ', v6)}");
                geometryData.Add($"v {String.Join(' ', v7)}");

                geometryData.Add($"f {counter}//1 {counter + 1}//1 {counter + 2}//1 {counter + 3}//1");
                geometryData.Add($"f {counter + 3}//1 {counter + 2}//1 {counter + 1}//1 {counter}//1");
                geometryData.Add($"f {counter + 4}//2 {counter + 5}//2 {counter + 6}//2 {counter + 7}//2");
                geometryData.Add($"f {counter + 7}//2 {counter + 6}//2 {counter + 5}//2 {counter + 4}//2");
                geometryData.Add($"f {counter}//3 {counter + 3}//3 {counter + 7}//3 {counter + 4}//3");
                geometryData.Add($"f {counter + 4}//3 {counter + 7}//3 {counter + 3}//3 {counter}//3");
                geometryData.Add($"f {counter + 1}//4 {counter + 2}//4 {counter + 6}//4 {counter + 5}//4");
                geometryData.Add($"f {counter + 5}//4 {counter + 6}//4 {counter + 2}//4 {counter + 1}//4");
                geometryData.Add($"f {counter + 3}//5 {counter + 2}//5 {counter + 6}//5 {counter + 7}//5");
                geometryData.Add($"f {counter + 7}//5 {counter + 6}//5 {counter + 2}//5 {counter + 3}//5");
                geometryData.Add($"f {counter + 1}//6 {counter}//6 {counter + 4}//6 {counter + 5}//6");
                geometryData.Add($"f {counter + 5}//6 {counter + 4}//6 {counter}//6 {counter + 1}//6");

                counter += 8;
            }
            return geometryData;
        }
        public List<string> generateCalorimetryTowers(List<CalorimetryData> inputData)
        {
            List<string> geometryData = new List<string>();
            int counter = 1;

            geometryData.Add("vn -1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 -1.0000\nvn 1.0000 -0.0000 -0.0000\nvn -0.0000 -0.0000 1.0000\nvn -0.0000 -1.0000 -0.0000\nvn -0.0000 1.0000 -0.0000");

            var V = Vector<double>.Build;

            foreach (CalorimetryData box in inputData)
            {
                var v0 = V.DenseOfArray(box.front_1);
                var v1 = V.DenseOfArray(box.front_2);
                var v2 = V.DenseOfArray(box.front_3);
                var v3 = V.DenseOfArray(box.front_4);
                var v4 = V.DenseOfArray(box.back_1);
                var v5 = V.DenseOfArray(box.back_2);
                var v6 = V.DenseOfArray(box.back_3);
                var v7 = V.DenseOfArray(box.back_4);

                v4 -= v0;
                v5 -= v1;
                v6 -= v2;
                v7 -= v3;

                double v4mag = v4.L2Norm();
                double v5mag = v5.L2Norm();
                double v6mag = v6.L2Norm();
                double v7mag = v7.L2Norm();

                v4 /= v4mag;
                v5 /= v5mag;
                v6 /= v6mag;
                v7 /= v7mag;
                double scale = (box.energy / box.scale);
                v4 *= (box.scale);
                v5 *= (box.scale);
                v6 *= (box.scale);
                v7 *= (box.scale);

                v4 += v0;
                v5 += v1;
                v6 += v2;
                v7 += v3;

                geometryData.Add($"v {String.Join(' ', v0)}");
                geometryData.Add($"v {String.Join(' ', v1)}");
                geometryData.Add($"v {String.Join(' ', v2)}");
                geometryData.Add($"v {String.Join(' ', v3)}");
                geometryData.Add($"v {String.Join(' ', v4)}");
                geometryData.Add($"v {String.Join(' ', v5)}");
                geometryData.Add($"v {String.Join(' ', v6)}");
                geometryData.Add($"v {String.Join(' ', v7)}");

                geometryData.Add($"f {counter}//1 {counter + 1}//1 {counter + 2}//1 {counter + 3}//1");
                geometryData.Add($"f {counter + 3}//1 {counter + 2}//1 {counter + 1}//1 {counter}//1");
                geometryData.Add($"f {counter + 4}//2 {counter + 5}//2 {counter + 6}//2 {counter + 7}//2");
                geometryData.Add($"f {counter + 7}//2 {counter + 6}//2 {counter + 5}//2 {counter + 4}//2");
                geometryData.Add($"f {counter}//3 {counter + 3}//3 {counter + 7}//3 {counter + 4}//3");
                geometryData.Add($"f {counter + 4}//3 {counter + 7}//3 {counter + 3}//3 {counter}//3");
                geometryData.Add($"f {counter + 1}//4 {counter + 2}//4 {counter + 6}//4 {counter + 5}//4");
                geometryData.Add($"f {counter + 5}//4 {counter + 6}//4 {counter + 2}//4 {counter + 1}//4");
                geometryData.Add($"f {counter + 3}//5 {counter + 2}//5 {counter + 6}//5 {counter + 7}//5");
                geometryData.Add($"f {counter + 7}//5 {counter + 6}//5 {counter + 2}//5 {counter + 3}//5");
                geometryData.Add($"f {counter + 1}//6 {counter}//6 {counter + 4}//6 {counter + 5}//6");
                geometryData.Add($"f {counter + 5}//6 {counter + 4}//6 {counter}//6 {counter + 1}//6");

                counter += 8;
            }
            return geometryData;
        }
        public void setScales()
        {
            //Hadronic scaling factor is equivalent to the largest energy value in each respective set (HE,HB,HO,HF)
            List<string> CALSETS = new List<string>() { "HERecHits_V2", "HBRecHits_V2", "HFRecHits_V2", "HORecHits_V2", "EBRecHits_V2", "ESRecHits_V2", "EERecHits_V2" };
            foreach (string CALSET in CALSETS)
            {
                var collection = data["Collections"][CALSET];

                if (collection.HasValues == false)
                {
                    continue;
                }

                List<double> energies = new List<double>();
                foreach (var item in collection)
                {
                    energies.Add((double)item[0].Value<double>());
                }

                double scaleEnergy = energies.ToArray().Max();

                switch (CALSET)
                {
                    case "HERecHits_V2":
                        HESCALE = scaleEnergy;
                        break;
                    case "HBRecHits_V2":
                        HBSCALE = scaleEnergy;
                        break;
                    case "HFRecHits_V2":
                        HFSCALE = scaleEnergy;
                        break;
                    case "HORecHits_V2":
                        HOSCALE = scaleEnergy;
                        break;
                    case "EBRecHits_V2":
                        EBSCALE = scaleEnergy;
                        break;
                    case "EERecHits_V2":
                        EESCALE = scaleEnergy;
                        break;
                    case "ESRecHits_V2":
                        ESSCALE = scaleEnergy;
                        break;
                }
            }
        }
        public List<RecHitFraction> recHitFractionsParse()
        {
            List<RecHitFraction> dataList = new List<RecHitFraction>();
            foreach (var item in data["Collections"]["RecHitFractions_V1"])
            {
                RecHitFraction thing = new RecHitFraction();
                var children = item.Children().Values<double>().ToList();
                thing.detid = (int)children[0];
                thing.fraction = children[1];
                thing.front_1 = new[] { children[2], children[3], children[4] };
                thing.front_2 = new[] { children[5], children[6], children[7] };
                thing.front_3 = new[] { children[8], children[9], children[10] };
                thing.front_4 = new[] { children[11], children[12], children[13] };
                thing.back_1 = new[] { children[14], children[15], children[16] };
                thing.back_2 = new[] { children[17], children[18], children[19] };
                thing.back_3 = new[] { children[20], children[21], children[22] };
                thing.back_4 = new[] { children[23], children[24], children[25] };
                dataList.Add(thing);
            }
            return dataList;
        }
        public List<List<RecHitFraction>> assignRecHitFractions(List<RecHitFraction> extras)
        {
            List<List<RecHitFraction>> dataList = new List<List<RecHitFraction>>();
            int indexer = 0;
            foreach (var item in data["Associations"]["SuperClusterRecHitFractions_V1"])
            {
                int index = item[0][1].Value<int>();
                if (dataList.Count() < index + 1)
                {
                    List<RecHitFraction> h = new List<RecHitFraction>();
                    dataList.Add(h);
                }
                dataList[index].Add(extras[indexer]);
                indexer++;
            }
            return dataList;
        }
        public List<SuperCluster> superClusterParse()
        {
            List<SuperCluster> dataList = new List<SuperCluster>();
            int idNumber = 0;

            foreach (var item in data["Collections"]["SuperClusters_V1"])
            {
                SuperCluster cluster = new SuperCluster();
                var values = item.Children().Values<string>().ToArray();

                cluster.id = idNumber;
                cluster.energy = Double.Parse(values[0]);
                cluster.pos = new[] { Double.Parse(values[1]), Double.Parse(values[2]), Double.Parse(values[3]) };
                cluster.eta = Double.Parse(values[4]);
                cluster.phi = Double.Parse(values[5]);
                cluster.algo = values[6];
                cluster.etaWidth = Double.Parse(values[7]);
                cluster.phiWidth = Double.Parse(values[8]);
                cluster.rawEnergy = Double.Parse(values[9]);
                cluster.preshowerEnergy = Double.Parse(values[10]);

                dataList.Add(cluster);
                idNumber++;
            }
            return dataList;
        }
        public void makeSuperClusters()
        {
            List<string> dataList = new List<string>();
            List<string> faces = new List<string>();
            int index = 0;
            int counter = 1;
            foreach (var item in recHitFractions)
            {
                dataList.Add($"o SuperCluster_{index}");

                foreach (RecHitFraction hit in item)
                {
                    dataList.Add($"v {String.Join(' ', hit.front_1)}");
                    dataList.Add($"v {String.Join(' ', hit.front_2)}");
                    dataList.Add($"v {String.Join(' ', hit.front_3)}");
                    dataList.Add($"v {String.Join(' ', hit.front_3)}");
                    dataList.Add($"v {String.Join(' ', hit.front_4)}");
                    dataList.Add($"v {String.Join(' ', hit.front_1)}");
                    /*dataList.Add($"v {String.Join(' ', hit.back_1)}");
                    dataList.Add($"v {String.Join(' ', hit.back_2)}");
                    dataList.Add($"v {String.Join(' ', hit.back_3)}");
                    dataList.Add($"v {String.Join(' ', hit.back_4)}");*/

                    faces.Add($"f {counter} {counter + 1} {counter + 2}");
                    faces.Add($"f {counter + 2} {counter + 1} {counter}");
                    faces.Add($"f {counter + 3} {counter + 4} {counter + 5}");
                    faces.Add($"f {counter + 5} {counter + 4} {counter + 3}");
                    counter += 6;
                }
                dataList.AddRange(faces);
                faces.Clear();
                index++;
            }
            File.WriteAllLines($"{eventTitle}//$_Superclusters.obj", dataList);
        }
        List<Vertex> vertexParse()
        {
            List<Vertex> dataList = new List<Vertex>();

            foreach (var item in data["Collections"]["Vertices_V1"])
            {
                var children = item.Children().Values<double>().ToList();
                Vertex vertex = new Vertex();

                vertex.isValid = (int)children[0];
                vertex.isFake = (int)children[1];
                vertex.pos = new double[] { children[2], children[3], children[4] };
                vertex.xError = children[5];
                vertex.yError = children[6];
                vertex.zError = children[7];
                vertex.chi2 = children[8];
                vertex.ndof = children[9];

                dataList.Add(vertex);
            }

            return dataList;
        }

        public static void GenerateOBJ(List<(Point3D center, Point3D width)> ellipsoids, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int vertexCount = 1;
                int objectCount = 1;
                int current = 0;

                foreach (var ellipsoid in ellipsoids)
                {
                    writer.WriteLine($"o Object{objectCount++}");

                    // Generate vertices
                    List<Point3D> vertices = GenerateEllipsoidVertices(ellipsoid.center, ellipsoid.width);

                    // Write vertices
                    foreach (var vertex in vertices)
                    {
                        writer.WriteLine($"v {vertex.X} {vertex.Y} {vertex.Z}");
                    }

                    // Write faces
                    int n = (int)Math.Sqrt(vertices.Count); // Assuming square grid
                    for (int i = 0; i < n - 1; i++)
                    {
                        for (int j = 0; j < n - 1; j++)
                        {
                            int currentIndex = i * n + j + vertexCount;
                            int nextIndex = currentIndex + 1;
                            int bottomIndex = currentIndex + n;
                            int nextBottomIndex = nextIndex + n;

                            writer.WriteLine($"f {currentIndex} {nextIndex} {nextBottomIndex} {bottomIndex}");
                        }

                    }
                    vertexCount += vertices.Count;
                }
            }

            Console.WriteLine("OBJ file generated successfully.");
        }

        private static List<Point3D> GenerateEllipsoidVertices(Point3D center, Point3D width)
        {
            List<Point3D> vertices = new List<Point3D>();

            int segments = 20; // Adjust as needed for smoother ellipsoids
            double thetaStep = 2 * Math.PI / segments;
            double phiStep = Math.PI / segments;

            for (int i = 0; i <= segments; i++)
            {
                double theta = i * thetaStep;
                for (int j = 0; j <= segments; j++)
                {
                    double phi = j * phiStep;
                    double x = center.X + width.X * Math.Sin(phi) * Math.Cos(theta);
                    double y = center.Y + width.Y * Math.Sin(phi) * Math.Sin(theta);
                    double z = center.Z + width.Z * Math.Cos(phi);
                    vertices.Add(new Point3D(x, y, z));
                }
            }

            return vertices;
        }
        void GenerateEllipsoidObj(string filePath, List<Vertex> vertexList, double sigmaFactor)
        {
            int vertexNumber = 0;
            int indexer = 0;
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var item in vertexList)
                {
                    writer.WriteLine($"o Vertex_{vertexNumber}");
                    double[] pos = item.pos;
                    double xDiameter = sigmaFactor * item.xError; double yDiameter = sigmaFactor * item.yError; double zDiameter = sigmaFactor * item.zError;

                    int index = 0;
                    // Generate vertices
                    int numVertices = 100;
                    for (int i = 0; i < numVertices; i++)
                    {
                        double theta = 2.0 * Math.PI * i / numVertices;
                        for (int j = 0; j < numVertices / 2; j++)
                        {
                            double phi = Math.PI * j / (numVertices / 2);

                            double x = pos[0] + xDiameter * Math.Sin(phi) * Math.Cos(theta);
                            double y = pos[1] + yDiameter * Math.Sin(phi) * Math.Sin(theta);
                            double z = pos[2] + zDiameter * Math.Cos(phi);

                            writer.WriteLine($"v {x} {y} {z}");
                        }
                    }

                    int vIndex = 1; // Vertex indices start from 1 in OBJ format
                    for (int i = 0; i < numVertices; i++)
                    {
                        for (int j = 0; j < numVertices / 2 - 1; j++)
                        {
                            int v1 = vIndex + j;
                            int v2 = vIndex + (j + 1) % (numVertices / 2);
                            int v3 = vIndex + (j + 1) % (numVertices / 2) + numVertices / 2;
                            int v4 = vIndex + j + numVertices / 2;

                            writer.WriteLine($"f {indexer + v1} {indexer + v2} {indexer + v3} {indexer + v4}");
                            //writer.WriteLine($"f {indexer + v1} {indexer + v3} {indexer + v4}");
                            index = v4;
                        }
                        vIndex += numVertices / 2;
                    }
                    indexer += index;
                    vertexNumber++;
                }
            }
        }
    }
    struct Point3D
    {
        public double X;
        public double Y;
        public double Z;

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    
}
