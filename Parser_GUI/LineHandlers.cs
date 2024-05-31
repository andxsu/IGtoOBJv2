using System;
using MathNet.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Security.AccessControl;
using System.IO;
using System.Threading.Tasks.Dataflow;
namespace Parser_GUI
{
    internal class IGTracks
    {
        //Properties
        protected string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//******
        protected string eventTitle;
        public string jsonData;
        protected JObject data { get; set; }
        private List<TrackExtrasData> trackExtrasData { get; set; }
        private List<TrackExtrasData> subTrackExtras { get; set; }//Extras corresponding to "Tracks_V3" data points
        private List<TrackExtrasData> standaloneMuonExtras { get; set; }
        private List<List<double[]>> standaloneMuonPoints;
        private List<TrackExtrasData> globalMuonExtras;
        private List<List<double[]>> globalMuonPoints;
        private List<TrackExtrasData> trackerMuonExtras { get; set; }
        private List<List<double[]>> trackerMuonPoints;
        private List<TrackExtrasData> electronExtras { get; set; }
        public List<StandaloneMuonData> standaloneMuonDatas { get; set; }
        public List<GlobalMuonData> globalMuonDatas { get; set; }
        public List<TrackerMuonData> trackerMuonDatas { get; set; }
        public List<GsfElectron> electronDatas { get; set; }
        public List<Track> trackDatas { get; set; }
        public List<string> filePaths { get; set; }
        //Constructor
        public IGTracks(JObject inputData, string name)
        {
            data = inputData;

            eventTitle = name;

            if (!Directory.Exists($"{eventTitle}"))
            {
                Directory.CreateDirectory($"{eventTitle}");
            }
            Execute();
            SerializeMET();
        }
        //Main Class Method
        public void Execute()
        {
            //Jesus christ this is so ugly, desperately need to replace all of these non void functions with void, in place methods
            var photonlist = photonParse();
            generatePhotonModels(photonlist);

            trackExtrasData = trackExtrasParse();
            globalMuonDatas = globalMuonParse();
            makeGlobalMuons();

            trackerMuonDatas = trackerMuonParse();
            makeTrackerMuons();

            standaloneMuonDatas = standaloneMuonParse();
            makeStandaloneMuons();

            var tracklist = tracksParse();
            removeMuonsFromTracks();
            makeTracks();
            trackDatas = trackDataParse();

            electronDatas = electronParse();
            makeElectrons();

        }
        //Methods
        public List<PhotonData> photonParse()
        {
            List<PhotonData> dataList = new List<PhotonData>();
            int idNumber = 0;

            foreach (var igPhotonData in data["Collections"]["Photons_V1"])
            {
                PhotonData currentPhotonItem = new PhotonData();

                var children = igPhotonData.Children().Values<double>().ToArray();

                currentPhotonItem.id = idNumber;
                currentPhotonItem.energy = children[0];
                currentPhotonItem.et = children[1];
                currentPhotonItem.eta = children[2];
                currentPhotonItem.phi = children[3];
                currentPhotonItem.position = new Vector3((float)children[4], (float)children[5], (float)children[6]);

                idNumber++;
                dataList.Add(currentPhotonItem);
            }
            return dataList;
        }
        private string makePhoton(PhotonData inputData)
        {
            double lEB = 3.0; //half-length of ECAL barrel in meters
            double rEB = 1.24; //radius of ECAL barrel in meters
            double eta = inputData.eta;
            double phi = inputData.phi;
            double px = Math.Cos(phi);
            double py = Math.Sin(phi);
            double pz = Math.Sinh(eta);
            double x0 = inputData.position.X;
            double y0 = inputData.position.Y;
            double z0 = inputData.position.Z;
            double t;

            if (Math.Abs(eta) > 1.48)
            {
                t = Math.Abs((lEB - z0) / pz);
            }
            else
            {
                double a = px * px + py * py;
                double b = 2 * x0 * px + 2 * y0 * py;
                double c = x0 * x0 + y0 * y0 - rEB * rEB;
                t = (-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            }

            string Contents;
            Contents = $"v {x0} {y0} {z0}\nv {x0 + 0.001} {y0 + 0.001} {z0 + 0.001}\nv {x0 + px * t} {y0 + py * t} {z0 + pz * t}\nv {x0 + px * t + 0.001} {y0 + py * t + 0.001} {z0 + pz * t + 0.001}";
            //Output a string of obj vectors that define the photon path
            return Contents;
        }
        public void generatePhotonModels(List<PhotonData> dataList)
        {
            //Write obj files for the photons
            List<string> dataStrings = new List<string>();
            int counter = 1;

            foreach (var igPhotonData in dataList)
            {
                string objData = makePhoton(igPhotonData);
                //Hey! That's the function from above!
                dataStrings.Add(objData);
                dataStrings.Add($"f {counter} {counter + 1} {counter + 3} {counter + 2}");
                counter += 4;
            }

            File.WriteAllText($"{eventTitle}\\8_Photons_V1.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\8_Photons_V1.obj", dataStrings);
        }
        public List<string> trackCubicBezierCurve(List<TrackExtrasData> inputData, string objectName)
        {
            //Same as the above function, except it outputs all the tracks into their own singular file. This allows me to keep Tracks_V3 as its 
            //own single file since it doesn't need to be matched to data.
            List<string> dataList = new List<string>();
            List<string> testList = new List<string>();
            List<int> exclusion_indeces = new List<int>();
            int numVerts = 32;
            int n = 0;
            int num = 0;
            int j = 0;
            foreach (var item in inputData)
            {
                testList.Clear();
                dataList.Add($"o {objectName}_{num}");
                for (double i = 0.0; i <= numVerts; i++)
                {

                    double t = (double)(i) / (double)(numVerts);

                    double t1 = Math.Pow(1.0 - t, 3);
                    double t2 = 3 * t * Math.Pow(1.0 - t, 2);
                    double t3 = 3 * t * t * (1.0 - t);
                    double t4 = Math.Pow(t, 3);

                    // Check out the wikipedia page for bezier curves if you want to understand the math. That's where I learned it!
                    // also we're using double arrays because i dont like Vector3 and floats. I'm the one who has to go through the headaches of working with double arrays
                    // instead of Vector3 so i get to make that call. i also wrote this before i realized i couldn't avoid using MathNET and i can't be bothered to 
                    // change it such that it uses MathNET vectors

                    // UPDATE: I should have refactored to use MathNET for everything when I had the chance

                    double[] term1 = { t1 * item.pos1[0], t1 * item.pos1[1], t1 * item.pos1[2] };
                    double[] term2 = { t2 * item.pos3[0], t2 * item.pos3[1], t2 * item.pos3[2] };
                    double[] term3 = { t3 * item.pos4[0], t3 * item.pos4[1], t3 * item.pos4[2] };
                    double[] term4 = { t4 * item.pos2[0], t4 * item.pos2[1], t4 * item.pos2[2] };
                    double[] point = { term1[0] + term2[0] + term3[0] + term4[0], term1[1] + term2[1] + term3[1] + term4[1], term1[2] + term2[2] + term3[2] + term4[2] };

                    string poin_t = $"v {point[0]} {point[1]} {point[2]}";
                    string point_t2 = $"v {point[0]} {point[1] + 0.001} {point[2]}";

                    dataList.Add(poin_t); dataList.Add(point_t2);
                    testList.Add(poin_t); testList.Add(point_t2);
                    n += 2;
                }
                for (int r = 1; r < (numVerts * 2); r++)
                {
                    string faces1 = $"f {r + (j * 33)} {r + 1 + (j * 33)} {r + 3 + (j * 33)} {r + 2 + (j * 33)}";
                    string faces2 = $"f {r + 2 + (j * 33)} {r + 3 + (j * 33)} {r + 1 + (j * 33)} {r + (j * 33)}";
                    dataList.Add(faces1); dataList.Add(faces2);
                }
                num++;
                j += 2;
                exclusion_indeces.Add(n);
            }

            return dataList;
        }
        public List<TrackExtrasData> trackExtrasParse()
        {
            List<TrackExtrasData> dataList = new List<TrackExtrasData>();

            foreach (var igTrackExtra in data["Collections"]["Extras_V1"])
            {
                TrackExtrasData currentItem = new TrackExtrasData();

                var children = igTrackExtra.Children().Values<double>().ToArray();

                currentItem.pos1 = new double[3] { children[0], children[1], children[2] };

                double dir1mag = Math.Sqrt(  //dir1mag and dir2mag are for making sure the direction vectors are normalized
                    Math.Pow(children[3], 2) +
                    Math.Pow(children[4], 2) +
                    Math.Pow(children[5], 2)
                );
                currentItem.dir1 = new double[3] { children[3] / dir1mag, children[4] / dir1mag, children[5] / dir1mag };

                currentItem.pos2 = new double[3] { children[6], children[7], children[8] };

                double dir2mag = Math.Sqrt(
                    Math.Pow(children[9], 2) +
                    Math.Pow(children[10], 2) +
                    Math.Pow(children[11], 2)
                    );
                currentItem.dir2 = new double[3] { children[9] / dir2mag, children[10] / dir2mag, children[11] / dir2mag };

                double distance = Math.Sqrt(
                    Math.Pow((currentItem.pos1[0] - currentItem.pos2[0]), 2) +
                    Math.Pow(currentItem.pos1[1] - currentItem.pos2[1], 2) +
                    Math.Pow(currentItem.pos1[2] - currentItem.pos2[2], 2)
                     );

                double scale = distance * 0.25;

                currentItem.pos3 = new double[3] { children[0] + scale * currentItem.dir1[0], children[1] + scale * currentItem.dir1[1], children[2] + scale * currentItem.dir1[2] };
                currentItem.pos4 = new double[3] { children[6] - scale * currentItem.dir2[0], children[7] - scale * currentItem.dir2[1], children[8] - scale * currentItem.dir2[2] };

                dataList.Add(currentItem);
            }
            return dataList;
        }
        public List<GlobalMuonData> globalMuonParse()
        {
            List<GlobalMuonData> dataList = new List<GlobalMuonData>();
            int idNumber = 0;

            var assocs = data["Associations"]["MuonGlobalPoints_V1"];

            if (assocs == null || assocs.HasValues == false)
            {
                return dataList;
            }

            foreach (var item in data["Collections"]["GlobalMuons_V1"])
            {
                GlobalMuonData muonData = new GlobalMuonData();
                var children = item.Children().Values<double>().ToArray();

                muonData.id = idNumber;
                muonData.pt = children[0];
                muonData.charge = (int)children[1];
                muonData.position = new double[] { children[2], children[3], children[4] };
                muonData.phi = children[5];
                muonData.eta = children[6];
                muonData.caloEnergy = children[7];

                idNumber++;
                dataList.Add(muonData);
            }
            int firstassoc = assocs[0][1][1].Value<int>();
            globalMuonPoints = makeTrackPoints(assocs);

            return dataList;
        }
        public void makeGlobalMuons()
        {
            if (globalMuonPoints == null) { return; }
            makeGeometryFromPoints(globalMuonPoints, "2_globalMuons", "globalMuons");
        }
        public List<TrackerMuonData> trackerMuonParse()
        {
            List<TrackerMuonData> dataList = new List<TrackerMuonData>();
            int idNumber = 0;

            var assocsExtras = data["Associations"]["MuonTrackerExtras_V1"];
            var assocsPoints = data["Associations"]["MuonTrackerPoints_V1"];
            if ((assocsExtras == null || assocsExtras.HasValues == false) && (assocsPoints == null || assocsPoints.HasValues == false))
            {
                trackerMuonExtras = new List<TrackExtrasData>();
                return dataList;
            }

            foreach (var item in data["Collections"]["TrackerMuons_V1"])
            {
                TrackerMuonData muonData = new TrackerMuonData();
                var children = item.Children().Values<double>().ToArray();

                muonData.id = idNumber;
                muonData.pt = children[0];
                muonData.charge = (int)children[1];
                muonData.position = new double[] { children[2], children[3], children[4] };
                muonData.phi = children[5];
                muonData.eta = children[6];

                idNumber++;
                dataList.Add(muonData);
            }
            if (assocsExtras.Count() >= 1)
            {
                int firstassoc = assocsExtras[0][1][1].Value<int>();
                trackerMuonExtras = trackExtrasData.GetRange(firstassoc, assocsExtras.Last()[1][1].Value<int>() - firstassoc + 1);
            }
            else
            {
                trackerMuonExtras = new List<TrackExtrasData>();
                trackerMuonExtras.Clear();
            }
            if (assocsPoints.HasValues)
            {
                trackerMuonPoints = makeTrackPoints(assocsPoints);
            }

            return dataList;
        }
        public void makeTrackerMuons()
        {
            if (trackerMuonExtras == null && trackerMuonPoints == null)
            {
                File.WriteAllText($"{eventTitle}\\1_trackerMuons.obj", String.Empty);
                return;
            }
            if (trackerMuonPoints == null)
            {
                List<string> dataList = trackCubicBezierCurve(trackerMuonExtras, "TrackerMuons");
                File.WriteAllText($"{eventTitle}\\1_TrackerMuons.obj", String.Empty);
                File.WriteAllLines($"{eventTitle}\\1_TrackerMuons.obj", dataList);
            }
            else
            {
                makeGeometryFromPoints(trackerMuonPoints, "3_TrackerMuons", "TrackerMuons");
            }
        }
        public List<StandaloneMuonData> standaloneMuonParse()
        {
            List<StandaloneMuonData> dataList = new List<StandaloneMuonData>();
            int idNumber = 0;
            var assocs = data["Associations"]["MuonTrackExtras_V1"];
            var assocsPoints = data["Associations"]["MuonStandalonePoints_V1"];
            if ((assocs == null || assocs.HasValues == false) && (assocsPoints == null || assocsPoints.HasValues == false))
            {
                return dataList;
            }
            foreach (var item in data["Collections"]["Tracks_V3"])
            {
                StandaloneMuonData muon = new StandaloneMuonData();
                var children = item.Children().Values<double>().ToArray();
                muon.id = idNumber;
                muon.pt = children[0];
                muon.charge = (int)children[1];
                muon.position = new double[] { children[2], children[3], children[4] };
                muon.phi = children[5];
                muon.eta = children[6];
                muon.caloEnergy = children[7];

                idNumber++;
                dataList.Add(muon);

            }
            int firstassoc = assocs[0][1][1].Value<int>();
            standaloneMuonExtras = trackExtrasData.GetRange(firstassoc, assocs.Last()[1][1].Value<int>() - firstassoc + 1);
            try { standaloneMuonPoints = makeTrackPoints(assocsPoints); } catch (Exception ex) { }

            return dataList;
        }
        public void makeStandaloneMuons()
        {
            if (standaloneMuonExtras == null && standaloneMuonPoints == null)
            {
                File.WriteAllText($"{eventTitle}\\3_standaloneMuons.obj", String.Empty);
                return;
            }
            if (standaloneMuonPoints == null)
            {
                List<string> dataList = trackCubicBezierCurve(standaloneMuonExtras, "standaloneMuons");
                File.WriteAllText($"{eventTitle}\\3_standaloneMuons.obj", String.Empty);
                File.WriteAllLines($"{eventTitle}\\3_standaloneMuons.obj", dataList);
            }
            else
            {
                makeGeometryFromPoints(standaloneMuonPoints, "3_standaloneMuons", "standaloneMuons");
            }
        }
        public List<Track> tracksParse()
        {
            List<Track> dataList = new List<Track>();
            var assocs = data["Associations"]["TrackExtras_V1"];

            if (assocs == null || assocs.HasValues == false)
            {
                return dataList;
            }
            foreach (var item in data["Collections"]["Tracks_V3"])
            {
                Track track = new Track();
                var children = item.Children().Values<double>().ToArray();

                track.pos = new double[] { children[0], children[1], children[2] };
                track.dir = new double[] { children[3], children[4], children[5] };
                track.pt = children[6];
                track.phi = children[7];
                track.eta = children[8];
                track.charge = (int)children[9];
                track.chi2 = children[10];
                track.ndof = children[11];
                dataList.Add(track);
            }
            int firstassoc = assocs[0][1][1].Value<int>();
            subTrackExtras = trackExtrasData.GetRange(firstassoc, assocs.Last()[1][1].Value<int>() - firstassoc + 1);

            return dataList;
        }
        public void makeTracks()
        {
            if (subTrackExtras == null) { return; }
            List<string> dataList = trackCubicBezierCurve(subTrackExtras, "Tracks");
            File.WriteAllText($"{eventTitle}\\9_Tracks.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\9_Tracks.obj", dataList);
        }
        public List<GsfElectron> electronParse()
        {
            List<GsfElectron> dataList = new List<GsfElectron>();
            int idNumber = 0;

            var assocs = data["Associations"]["GsfElectronExtras_V1"];

            if (assocs == null || assocs.HasValues == false)
            {
                return dataList;
            }

            foreach (var item in data["Collections"]["GsfElectrons_V2"])
            {
                GsfElectron electron = new GsfElectron();
                var children = item.Children().Values<double>().ToArray();

                electron.id = idNumber;
                electron.pt = children[0];
                electron.eta = children[1];
                electron.phi = children[2];
                electron.charge = (int)children[3];
                electron.pos = new double[] { children[4], children[5], children[6] };
                electron.dir = new double[] { children[7], children[8], children[9] };

                idNumber++;
                dataList.Add(electron);
            }
            int firstassoc = assocs[0][1][1].Value<int>();
            electronExtras = trackExtrasData.GetRange(firstassoc, assocs.Last()[1][1].Value<int>() - firstassoc + 1);

            return dataList;
        }
        public void makeElectrons()
        {
            if (electronExtras == null) { return; }
            List<string> dataList = trackCubicBezierCurve(electronExtras, "gsfElectrons");
            File.WriteAllText($"{eventTitle}\\4_gsfElectrons.obj", String.Empty);
            File.WriteAllLines($"{eventTitle}\\4_gsfElectrons.obj", dataList);
        }
        public METData METParse()
        {
            METData met = new METData();
            var metdata = data["Collections"]["PFMETs_V1"][0];
            var children = metdata.Values<double>().ToList();
            met.phi = children[0];
            met.pt = children[1];
            met.px = children[2];
            met.py = children[3];
            met.pz = children[4];

            return met;
        }
        public List<Track> trackDataParse()
        {
            int n = 0;
            List<Track> dataList = new List<Track>();
            foreach (var item in data["Collections"]["Tracks_V3"])
            {
                Track track = new Track();
                var children = item.Children().Values<double>().ToArray();

                track.id = n;
                track.pos = new double[] { children[0], children[1], children[2] };
                track.dir = new double[] { children[3], children[4], children[5] };
                track.pt = children[6];
                track.phi = children[7];
                track.eta = children[8];
                track.charge = (int)children[9];
                track.chi2 = children[10];
                track.ndof = children[11];
                dataList.Add(track);
                n++;
            }
            return dataList;
        }
        public void SerializeMET()
        {
            string metdata = JsonConvert.SerializeObject(METParse(), Formatting.Indented);
            File.WriteAllText($@"{eventTitle}/METData.json", metdata);
        }
        public void removeMuonsFromTracks()
        {
            foreach (TrackExtrasData muon in trackerMuonExtras)
            {
                var index = subTrackExtras.FindIndex(x => x.pos1[0] == muon.pos1[0]);
                if (index > -1)
                {
                    subTrackExtras.RemoveAt(index);
                }
            }
        }
        public List<List<double[]>> makeTrackPoints(JToken assoc)
        {
            List<List<double[]>> positions = new List<List<double[]>>();
            //var assoc = data["Associations"]["MuonGlobalPoints_V1"];
            var extras = data["Collections"]["Points_V1"];

            if (assoc == null || assoc.HasValues == false || extras.HasValues == false || extras == null)
            {
                return positions;
            }

            int mi;
            int pi;

            foreach (var item in assoc)
            {
                mi = item[0][1].Value<int>();
                pi = item[1][1].Value<int>();
                if (positions.Count() <= mi) { List<double[]> blank = new List<double[]>(); positions.Add(blank); }
                positions[mi].Add(extras[pi][0].ToObject<double[]>());
            }
            return positions;
        }
        public void makeGeometryFromPoints(List<List<double[]>> points, string path, string name)
        {
            List<List<string>> dataLists = new List<List<string>>();
            int accountingfactor = 0;
            List<string> strings = new List<string>();
            int counter = 0;
            foreach (var subitem in points)
            {
                List<string> medi = new List<string>();
                medi.Add($"o {name}_{counter}");
                foreach (var item in subitem)
                {
                    string line1 = $"v {item[0]} {item[1]} {item[2]}";
                    string line2 = $"v {item[0]} {item[1] + 0.001} {item[2]}";
                    medi.Add(line1); medi.Add(line2);
                }
                dataLists.Add(medi);
                counter++;
            }
            foreach (var item in dataLists)
            {
                var ble = item;
                int count = ble.Count();
                for (int i = 1; i < (count - 3); i += 2)
                {
                    string bleh = $"f {accountingfactor + i} {accountingfactor + i + 1} {accountingfactor + i + 2} {accountingfactor + i + 3}";
                    ble.Add(bleh);
                    string bleh1 = $"f {accountingfactor + i + 3} {accountingfactor + i + 2} {accountingfactor + i + 1} {accountingfactor + i}";
                    ble.Add(bleh1);
                }
                accountingfactor += count - 1;
                strings.AddRange(ble);
            }
            File.WriteAllLines(@$"{eventTitle}\{path}.obj", strings);
        }
    }
}

