using System;
using CsvHelper.Configuration.Attributes;
using System.Numerics;
namespace Parser_GUI
{
    struct MuonChamberData
    {
        public string name;
        public int detid;
        public int id;
        public double[] front_1;
        public double[] front_2;
        public double[] front_3;
        public double[] front_4;
        public double[] back_1;
        public double[] back_2;
        public double[] back_3;
        public double[] back_4;
        public double[] vertical;
        public double[] horizontal;
        public double[] centroid;
    }
    struct Vertex
    {
        public int isValid;
        public int isFake;
        public double[] pos;
        public double xError;
        public double yError;
        public double zError;
        public double chi2;
        public double ndof;
    }
    struct CalorimetryData
    {
        public double energy;
        public double eta;
        public double phi;
        public double time;
        public int detid;
        public double[] front_1;
        public double[] front_2;
        public double[] front_3;
        public double[] front_4;
        public double[] back_1;
        public double[] back_2;
        public double[] back_3;
        public double[] back_4;
        public double scale;
    }
    struct METData
    {
        public double phi;
        public double pt;
        public double px;
        public double py;
        public double pz;
    }
    struct JetData
    {
        public int id;
        public double et;
        public double eta;
        public double theta;
        public double phi;
    }
    struct GlobalMuonData
    {
        public int id;
        public double pt;
        public int charge;
        public double[] position;
        public double phi;
        public double eta;
        public double caloEnergy;
    }
    struct StandaloneMuonData
    {
        public int id;
        public double pt;
        public int charge;
        public double[] position;
        public double phi;
        public double eta;
        public double caloEnergy;
    }
    struct TrackerMuonData
    {
        public int id;
        public double pt;
        public int charge;
        public double[] position;
        public double phi;
        public double eta;
    }
    struct PhotonData
    {
        public int id;
        public string name;
        public double energy;
        public double et;
        public double eta;
        public double phi;
        public Vector3 position;
    }
    struct TrackExtrasData
    {
        public double[] pos1;
        public double[] dir1;
        public double[] pos2;
        public double[] dir2;
        public double[] pos3;
        public double[] pos4;
    }
    struct Track
    {
        public int id;
        public double[] pos;
        public double[] dir;
        public double pt;
        public double phi;
        public double eta;
        public int charge;
        public double chi2;
        public double ndof;
    }
    struct GsfElectron
    {
        public int id;
        public double pt;
        public double eta;
        public double phi;
        public int charge;
        public double[] pos;
        public double[] dir;
    }
    struct SuperCluster
    {
        public int id;
        public double energy;
        public double[] pos;
        public double eta;
        public double phi;
        public string algo;
        public double etaWidth;
        public double phiWidth;
        public double rawEnergy;
        public double preshowerEnergy;
    }
    struct RecHitFraction
    {
        public int detid;
        public double fraction;
        public double[] front_1;
        public double[] front_2;
        public double[] front_3;
        public double[] front_4;
        public double[] back_1;
        public double[] back_2;
        public double[] back_3;
        public double[] back_4;
    }
}

