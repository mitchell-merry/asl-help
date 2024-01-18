using AslHelp;
using AslHelp.Data.AutoSplitter;
using AslHelp.IO;
using AslHelp.SceneManagement;
using System.Data.SqlTypes;
using System.Dynamic;

public partial class Unity
{
    public bool IsLoading => Scenes is SceneManager mgr && mgr.Count > 1;

    public Module MonoModule { get; private set; }
    public Module UnityPlayer { get; private set; }
    public SceneManager Scenes { get; private set; }

    private string _dataFolder;
    public string DataFolder
    {
        get
        {
            string directory = Path.GetDirectoryName(MainModule.FilePath);
            string folderName = _dataFolder ?? Path.GetFileName(MainModule.FilePath)[..^4];

            return Path.Combine(directory, folderName);
        }
        set
        {
            Assert.InAction(nameof(DataFolder), "startup");

            Debug.Info($"  => Will use {value} as the DataFolder.");
            _dataFolder = value;
        }
    }

    private bool _loadSceneManager;
    public bool LoadSceneManager
    {
        get => _loadSceneManager;
        set
        {
            Assert.InAction("startup");

            Debug.Info(value ? "  => Will try to load SceneManager." : "  => Will not load SceneManager.");
            _loadSceneManager = value;
        }
    }

    private string[] _monoV1Modules = ["mono.dll"];
    public string[] MonoV1Modules
    {
        get => _monoV1Modules;
        set
        {
            Assert.InAction("startup");
            Assert.NotNull(value, nameof(MonoV1Modules));

            _monoV1Modules = value;
        }
    }

    private string[] _monoV2Modules = ["GameAssembly.dll"];
    public string[] MonoV2Modules
    {
        get => _monoV2Modules;
        set
        {
            Assert.InAction("startup");
            Assert.NotNull(value, nameof(MonoV2Modules));

            _monoV2Modules = value;
        }
    }

    private string[] _il2cppModules = ["GameAssembly.dll"];
    public string[] IL2CPPModules
    {
        get => _il2cppModules;
        set
        {
            Assert.InAction("startup");
            Assert.NotNull(value, nameof(IL2CPPModules));

            _il2cppModules = value;
        }
    }

    private Version _unityVersion;
    public Version UnityVersion
    {
        get
        {
            if (_unityVersion is not null)
            {
                return _unityVersion;
            }

            const string GGM = "globalgamemanagers";
            const string MD = "mainData";
            const string DU3D = "data.unity3d";

            Debug.Info("Retrieving Unity version...");

            string ggm = Path.Combine(DataFolder, GGM), md = Path.Combine(DataFolder, MD), du3d = Path.Combine(DataFolder, DU3D);
            bool ggmExists = File.Exists(ggm), mdExists = File.Exists(md), du3dExists = File.Exists(du3d);

            string ver = null;

            if (ggmExists || mdExists)
            {
                EndianBinaryReader reader = new(ggmExists ? ggm : md, true);

                uint metaDataSize = reader.ReadUInt32();
                uint fileSize = reader.ReadUInt32();
                uint version = reader.ReadUInt32();
                reader.ReadBytes(4);

                if (version >= 9)
                {
                    reader.ReadBytes(4);
                }
                else
                {
                    reader.BaseStream.Position = fileSize - metaDataSize;
                    reader.ReadByte();
                }

                if (version >= 22)
                {
                    reader.ReadBytes(28);
                }

                if (version >= 7)
                {
                    ver = reader.ReadString();

                    goto Success;
                }
            }
            else if (du3dExists)
            {
                EndianBinaryReader reader = new(du3d, true);

                reader.ReadString();
                reader.ReadBytes(4);
                reader.ReadString();

                ver = reader.ReadString();

                goto Success;
            }

            Debug.Warn("  => Version not found.");
            return null;

        Success:
            Debug.Info($"  => Unity {ver}.");
            Debug.Info($"  => Doesn't look right? You can set the helper's `{nameof(UnityVersion)}` manually in 'startup {{}}':");
            Debug.Info($"     `vars.Helper.UnityVersion = new Version(2017, 2);`");

            _unityVersion = stringToVersion(ver);
            return _unityVersion;

            static Version stringToVersion(string version)
            {
                version = version.Replace('f', '.');

                if (Version.TryParse(version, out Version result))
                {
                    return result;
                }
                else
                {
                    string[] split = version.Split('.');
                    return new(int.Parse(split[0]), int.Parse(split[1]));
                }
            }
        }
        set
        {
            Assert.InAction("startup");
            Assert.NotNull(value, nameof(UnityVersion));

            _unityVersion = value;
        }
    }

    private int _il2CppVersion;
    public int Il2CppVersion
    {
        get
        {
            if (_il2CppVersion != 0)
            {
                return _il2CppVersion;
            }

            const string IL2CPPD = "il2cpp_data";
            const string MD = "Metadata";
            const string GMD = "global-metadata.dat";

            Debug.Info("Retrieving IL2CPP version...");

            string gmd = Path.Combine(DataFolder, IL2CPPD, MD, GMD);

            if (File.Exists(gmd))
            {
                EndianBinaryReader reader = new(gmd);

                reader.ReadBytes(4);

                int ver = reader.ReadInt32();
                Debug.Info($"  => IL2CPP {ver}.");

                _il2CppVersion = ver;
                return ver;
            }

            Debug.Warn("  => Version not found.");

            return 0;
        }
    }
}
