#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Windows.Forms;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using UnityButton = UnityEngine.UI.Button;

public class MinecraftLauncher : MonoBehaviour
{
    public InputField javaPathInput;
    public InputField minecraftPathInput;
    public InputField usernameInput;
    public InputField widthInput;
    public InputField heightInput;
    public InputField gamett;
    public Text statusText;
    public UnityButton launchButton;
    public UnityButton javaBrowseButton;
    public UnityButton minecraftBrowseButton;
    public UnityButton selectSkinButton;
    public Image skinPreviewImage;

    public GameObject launchPanel;
    public GameObject settingsPanel;
    public UnityButton launchTabButton;
    public UnityButton settingsTabButton;
    public Dropdown languageDropdown;
    public Image backgroundImagePreview;
    public UnityButton selectBackgroundButton;
    public Dropdown versionDropdown;

    public Text launchButtonText;
    public Text settingsButtonText;
    public Text title;
    public Text ltitle;
    public Text text1;
    public Text text2;
    public Text gametitle;
    public GameObject modManagerPanel;
    public UnityButton modManagerButton;
    public UnityButton modBackButton;
    public Transform modListContainer;
    public GameObject modItemPrefab;
    public Dropdown javaPathDropdown;

    private List<GameObject> modItemPool = new List<GameObject>();

    private bool modPanelActive = false;
    private string modsFolderPath => Path.Combine(
    minecraftPathInput.text,
    "versions",
    selectedVersion,
    "mods"
);


    private string selectedVersion = "1.12.2";

    void Start()
    {
        if (string.IsNullOrEmpty(javaPathInput.text))
            javaPathInput.text = Path.Combine(Application.dataPath, "../Xenos/runtime/jre/bin/java.exe");
        if (string.IsNullOrEmpty(minecraftPathInput.text))
            minecraftPathInput.text = Path.Combine(Application.dataPath, "../Xenos/.minecraft");
        if (string.IsNullOrEmpty(usernameInput.text))
            usernameInput.text = "Xenossurround";
        if (string.IsNullOrEmpty(widthInput.text))
            widthInput.text = "1000";
        if (string.IsNullOrEmpty(heightInput.text))
            heightInput.text = "600";

        // 绑定按钮事件
        launchButton.onClick.AddListener(StartMinecraft);
        javaBrowseButton.onClick.AddListener(BrowseJavaPath);
        minecraftBrowseButton.onClick.AddListener(BrowseMinecraftFolder);
        launchTabButton.onClick.AddListener(() => SwitchTab(true));
        settingsTabButton.onClick.AddListener(() => SwitchTab(false));
        selectBackgroundButton.onClick.AddListener(SelectBackgroundImage);
        selectSkinButton.onClick.AddListener(SelectSkinFile);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        versionDropdown.onValueChanged.AddListener(OnVersionChanged);
        javaPathDropdown.onValueChanged.AddListener(OnJavaDropdownChanged);

        SwitchTab(true);

        string defaultBgPath = Path.Combine(Application.dataPath, "../Xenos/Background/bg.png");
        StartCoroutine(LoadImage(defaultBgPath));

        AutoDetectJava();

        // 加载版本列表
        versionDropdown.ClearOptions();
        var versions = GetAvailableVersions();
        versionDropdown.AddOptions(versions);
        selectedVersion = versions.Count > 0 ? versions[0] : "1.12.2";
        modManagerButton.onClick.AddListener(OpenModManager);
        modBackButton.onClick.AddListener(CloseModManager);
        modManagerButton.gameObject.SetActive(false);
        modManagerPanel.SetActive(false);

    }
    void SwitchTab(bool toLaunch)
    {
        launchPanel.SetActive(toLaunch);
        settingsPanel.SetActive(!toLaunch);

        modPanelActive = false;
        modManagerPanel.SetActive(false);
        modManagerButton.gameObject.SetActive(toLaunch && IsForgeOptiFine(selectedVersion));
    }
    
    void OpenModManager()
    {
        modPanelActive = true;

        launchPanel.SetActive(false);
        settingsPanel.SetActive(false);
        modManagerPanel.SetActive(true);

        modManagerButton.gameObject.SetActive(false);

        // 重建 mod 列表
        LoadModList();

        foreach (Transform child in modListContainer)
        {
            child.gameObject.SetActive(true);
        }
    }


    void CloseModManager()
    {
        modPanelActive = false;
        modManagerPanel.SetActive(false);
        launchPanel.SetActive(true); 
        modManagerButton.gameObject.SetActive(IsForgeOptiFine(selectedVersion));

        
    }
    void LoadModList()
    {
        foreach (var obj in modItemPool)
        {
            obj.SetActive(false);
        }

        if (!Directory.Exists(modsFolderPath))
            Directory.CreateDirectory(modsFolderPath);

        string[] modFiles = Directory.GetFiles(modsFolderPath, "*.jar*");

        for (int i = 0; i < modFiles.Length; i++)
        {
            string modPath = modFiles[i];
            string modFile = Path.GetFileName(modPath);
            bool isDisabled = modFile.EndsWith(".dsab");
            string displayName = isDisabled ? modFile.Substring(0, modFile.Length - 5) : modFile;

            GameObject item;

            if (i < modItemPool.Count)
            {
                item = modItemPool[i];
                item.SetActive(true);
            }
            else
            {
                item = Instantiate(modItemPrefab, modListContainer);
                modItemPool.Add(item);
            }

            item.GetComponentInChildren<Text>().text = displayName;

            Toggle toggle = item.GetComponentInChildren<Toggle>();
            toggle.isOn = !isDisabled;

            toggle.onValueChanged.RemoveAllListeners();

            string capturedModFile = modFile; // 捕获闭包变量

            toggle.onValueChanged.AddListener((enabled) =>
            {
                string srcPath = Path.Combine(modsFolderPath, capturedModFile);
                string newName;

                if (enabled && capturedModFile.EndsWith(".dsab"))
                {
                    newName = capturedModFile.Substring(0, capturedModFile.Length - 5);
                    File.Move(srcPath, Path.Combine(modsFolderPath, newName));
                }
                else if (!enabled && !capturedModFile.EndsWith(".dsab"))
                {
                    newName = capturedModFile + ".dsab";
                    File.Move(srcPath, Path.Combine(modsFolderPath, newName));
                }

                LoadModList(); // 刷新状态
            });
        }
    }

    void OnLanguageChanged(int index)
    {
        if (index == 0)
        {
            launchButtonText.text = "启动";
            settingsButtonText.text = "设置";
            launchTabButton.GetComponentInChildren<Text>().text = "启动";
            settingsTabButton.GetComponentInChildren<Text>().text = "设置";
            text1.text = "分辨率";
            text2.text = "用户名";
            title.text = "Xenos启动器";
            ltitle.text = "Xenos启动器";
            gametitle.text = "游戏标题";
        }
        else
        {
            launchButtonText.text = "Launch";
            settingsButtonText.text = "Settings";
            launchTabButton.GetComponentInChildren<Text>().text = "Launch";
            settingsTabButton.GetComponentInChildren<Text>().text = "Settings";
            text1.text = "Resolution";
            text2.text = "Username";
            title.text = "XenosLauncher";
            ltitle.text = "XenosLauncher";
            gametitle.text = "Game Title";
        }
    }

    void SelectBackgroundImage()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg";
            dialog.Title = "选择背景图";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                StartCoroutine(LoadImage(dialog.FileName));
            }
        }
#endif
    }

    System.Collections.IEnumerator LoadImage(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        backgroundImagePreview.sprite = sprite;
        backgroundImagePreview.preserveAspect = true;
        yield return null;
    }

    void BrowseJavaPath()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "Java Executable|java.exe|Java Executable for Window Subsystem|javaw.exe";
            dialog.Title = "选择 java.exe";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                javaPathInput.text = dialog.FileName;
            }
        }
#endif
    }

    void BrowseMinecraftFolder()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "选择 .minecraft 根目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                minecraftPathInput.text = dialog.SelectedPath;
            }
        }
#endif
    }

    void SelectSkinFile()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "PNG Skin|*.png";
            dialog.Title = "选择皮肤文件";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string skinPath = dialog.FileName;
                ApplySkin(skinPath);
            }
        }
#endif
    }
    public void openMods()
    {
        string path = Path.Combine(Application.dataPath, "../Xenos/.minecraft/versions/1.12.2-Forge_14.23.5.2847-OptiFine_G5/mods");
        Process.Start(path);
    }

    void ApplySkin(string path)
    {
        string username = string.IsNullOrWhiteSpace(usernameInput.text) ? "Xenossurround" : usernameInput.text;
        string mcRoot = minecraftPathInput.text;
        string destPath = Path.Combine(Application.dataPath, "../Xenos/.minecraft/versions/1.12.2-Forge_14.23.5.2847-OptiFine_G5/CustomSkinLoader/LocalSkin/skins");

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

        string finalSkinPath = Path.Combine(destPath, $"{username}.png");
        File.Copy(path, finalSkinPath, true);
        StartCoroutine(PreviewSkin(finalSkinPath));
        statusText.text = "皮肤已替换";
        FindObjectOfType<MCSkinBinder>()?.RefreshSkin();
    }

    System.Collections.IEnumerator PreviewSkin(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);
        skinPreviewImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        yield return null;
    }
    void AutoDetectJava()
    {
        List<string> foundJavaPaths = new List<string>();

        string[] commonPaths = new string[]
        {
        @"C:\Program Files\Java",
        @"C:\Program Files (x86)\Java"
        };

        foreach (string basePath in commonPaths)
        {
            if (Directory.Exists(basePath))
            {
                foreach (string dir in Directory.GetDirectories(basePath))
                {
                    string javaExe = Path.Combine(dir, "bin", "java.exe");
                    if (File.Exists(javaExe) && !foundJavaPaths.Contains(javaExe))
                        foundJavaPaths.Add(javaExe);
                }
            }
        }

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("where", "java")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process proc = Process.Start(psi))
            using (StreamReader reader = proc.StandardOutput)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (File.Exists(line) && !foundJavaPaths.Contains(line))
                        foundJavaPaths.Add(line);
                }
            }
        }
        catch { }



        if (foundJavaPaths.Count > 0)
        {
            javaPathDropdown.ClearOptions();

            javaPathDropdown.AddOptions(foundJavaPaths);

            javaPathDropdown.value = 0;
            javaPathDropdown.RefreshShownValue();

            // 同步到输入框
            javaPathInput.text = foundJavaPaths[0];

            statusText.text = $"找到 {foundJavaPaths.Count} 个 Java 安装路径";
        }
        else
        {
            javaPathDropdown.ClearOptions();
            javaPathDropdown.AddOptions(new List<string> { "未找到 Java" });
            javaPathDropdown.value = 0;
            javaPathDropdown.RefreshShownValue();
            statusText.text = "未检测到任何 Java 安装路径,请手动指定";
        }

    }

    void OnVersionChanged(int index)
    {
        selectedVersion = versionDropdown.options[index].text;
        modManagerButton.gameObject.SetActive(IsForgeOptiFine(selectedVersion));
    }

    void OnJavaDropdownChanged(int index)
    {
        string selectedPath = javaPathDropdown.options[index].text;
        javaPathInput.text = selectedPath;
    }

    List<string> GetAvailableVersions()
    {
        string mcRoot = minecraftPathInput.text;
        string versionsDir = Path.Combine(mcRoot, "versions");
        List<string> result = new List<string>();

        if (Directory.Exists(versionsDir))
        {
            foreach (string dir in Directory.GetDirectories(versionsDir))
            {
                string name = Path.GetFileName(dir);
                if (File.Exists(Path.Combine(dir, name + ".jar")))
                    result.Add(name);
            }
        }

        return result;
    }
    bool IsForgeOptiFine(string version)
    {
        return version.Contains("Forge") && version.Contains("OptiFine");
    }

    void StartMinecraft()
    {
        if (IsForgeOptiFine(selectedVersion))
        {
            StartForgeOptiFine();
        }
        else
        {
            StartVanilla();
        }
    }


    void StartVanilla()
    {
        string javaPath = string.IsNullOrWhiteSpace(javaPathInput.text)
            ? Path.Combine(Application.dataPath, "../Xenos/runtime/jre/bin/java.exe")
            : javaPathInput.text;

        string mcRoot = minecraftPathInput.text;
        string username = string.IsNullOrWhiteSpace(usernameInput.text) ? "Xenossurround" : usernameInput.text;
        string width = string.IsNullOrWhiteSpace(widthInput.text) ? "1000" : widthInput.text;
        string height = string.IsNullOrWhiteSpace(heightInput.text) ? "600" : heightInput.text;
        string version = selectedVersion;
        string gameDir = Path.Combine(mcRoot, "versions", version);
        string nativePath = Path.Combine(mcRoot, "versions", version, version + "-natives");
        string classPath = GetClasspath(mcRoot, version);
        string jarWrapper = Path.Combine(Application.dataPath, "../Xenos/JavaWrapper.jar");
        string uuid = "00000FFFFFFFFFFFFFFFFFFFFFFA6B93";
        string accessToken = "00000FFFFFFFFFFFFFFFFFFFFFFA6B93";
        string tt = string.IsNullOrWhiteSpace(gamett.text) ? "Minecraft-" + version + "-Xenos" : gamett.text;

        string javaArgs = $"-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow " +
            "-Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True " +
            "-Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true " +
            "-Xmn245m -Xmx1638m " +
            $"-Djava.library.path=\"{nativePath}\" -cp \"{classPath}\" " +
            "-Doolloo.jlw.tmpdir=\"\\Xenos\" " +
            $"-jar \"{jarWrapper}\" net.minecraft.client.main.Main " +
            $"--username {username} --version {version} " +
            $"--gameDir \"{gameDir}\" " +
            $"--assetsDir \"{Path.Combine(mcRoot, "assets")}\" --assetIndex 1.12 " +
            $"--uuid {uuid} --accessToken {accessToken} " +
            $"--userType msa --versionType XENOS --title {tt} --height {height} --width {width} --fullscreen";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = javaPath,
            Arguments = javaArgs,
            WorkingDirectory = gameDir,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        try
        {
            Process.Start(psi);
            statusText.text = "Minecraft 已启动";
        }
        catch (System.Exception ex)
        {
            statusText.text = "启动失败: " + ex.Message;
        }
    }

    void StartForgeOptiFine()
    {
        string javaPath = string.IsNullOrWhiteSpace(javaPathInput.text)
            ? Path.Combine(Application.dataPath, "../Xenos/runtime/jre/bin/java.exe")
            : javaPathInput.text;

        string mcRoot = minecraftPathInput.text;
        string version = selectedVersion;
        string username = string.IsNullOrWhiteSpace(usernameInput.text) ? "Xenossurround" : usernameInput.text;
        string width = string.IsNullOrWhiteSpace(widthInput.text) ? "1000" : widthInput.text;
        string height = string.IsNullOrWhiteSpace(heightInput.text) ? "600" : heightInput.text;
        string gameDir = Path.Combine(mcRoot, "versions", version);
        string nativePath = Path.Combine(gameDir, version + "-natives");
        string uuid = "00000FFFFFFFFFFFFFFFFFFFFFFA6B93";
        string accessToken = "00000FFFFFFFFFFFFFFFFFFFFFFA6B93";
        string tt = string.IsNullOrWhiteSpace(gamett.text) ? $"Minecraft-{version}-Forge" : gamett.text;

        string classPath = GetForgeOptiFineClassPath(mcRoot, version);
        string jarWrapper = Path.Combine(Application.dataPath, "../Xenos/JavaWrapper.jar");

        string javaArgs = $"-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow " +
            "-Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True " +
            "-Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true " +
            "-Xmn399m -Xmx2662m " +
            $"-Djava.library.path=\"{nativePath}\" -cp \"{classPath}\" " +
            $"-Doolloo.jlw.tmpdir=\"\\Xenos\" -jar \"{jarWrapper}\" net.minecraft.launchwrapper.Launch " +
            $"--username {username} --version {version} " +
            $"--gameDir \"{gameDir}\" " +
            $"--assetsDir \"{Path.Combine(mcRoot, "assets")}\" --assetIndex 1.12 " +
            $"--uuid {uuid} --accessToken {accessToken} " +
            "--userType msa --versionType Forge " +
            "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker " +
            $"--title {tt} --height {height} --width {width} --fullscreen";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = javaPath,
            Arguments = javaArgs,
            WorkingDirectory = gameDir,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        try
        {
            Process.Start(psi);
            statusText.text = "Forge端已启动";
        }
        catch (System.Exception ex)
        {
            statusText.text = "启动失败: " + ex.Message;
        }
    }

    public static string GetClasspath(string mcRoot, string version)
    {
        List<string> jars = new List<string>();

        string[] relativeJarPaths = new string[]
        {
            @"com\mojang\patchy\1.3.9\patchy-1.3.9.jar",
            @"oshi-project\oshi-core\1.1\oshi-core-1.1.jar",
            @"net\java\dev\jna\jna\4.4.0\jna-4.4.0.jar",
            @"net\java\dev\jna\platform\3.4.0\platform-3.4.0.jar",
            @"com\ibm\icu\icu4j-core-mojang\51.2\icu4j-core-mojang-51.2.jar",
            @"net\sf\jopt-simple\jopt-simple\5.0.3\jopt-simple-5.0.3.jar",
            @"com\paulscode\codecjorbis\20101023\codecjorbis-20101023.jar",
            @"com\paulscode\codecwav\20101023\codecwav-20101023.jar",
            @"com\paulscode\libraryjavasound\20101123\libraryjavasound-20101123.jar",
            @"com\paulscode\librarylwjglopenal\20100824\librarylwjglopenal-20100824.jar",
            @"com\paulscode\soundsystem\20120107\soundsystem-20120107.jar",
            @"io\netty\netty-all\4.1.9.Final\netty-all-4.1.9.Final.jar",
            @"com\google\guava\guava\21.0\guava-21.0.jar",
            @"org\apache\commons\commons-lang3\3.5\commons-lang3-3.5.jar",
            @"commons-io\commons-io\2.5\commons-io-2.5.jar",
            @"commons-codec\commons-codec\1.10\commons-codec-1.10.jar",
            @"net\java\jinput\jinput\2.0.5\jinput-2.0.5.jar",
            @"net\java\jutils\jutils\1.0.0\jutils-1.0.0.jar",
            @"com\google\code\gson\gson\2.8.0\gson-2.8.0.jar",
            @"com\mojang\authlib\1.5.25\authlib-1.5.25.jar",
            @"com\mojang\realms\1.10.22\realms-1.10.22.jar",
            @"org\apache\commons\commons-compress\1.8.1\commons-compress-1.8.1.jar",
            @"org\apache\httpcomponents\httpclient\4.3.3\httpclient-4.3.3.jar",
            @"commons-logging\commons-logging\1.1.3\commons-logging-1.1.3.jar",
            @"org\apache\httpcomponents\httpcore\4.3.2\httpcore-4.3.2.jar",
            @"it\unimi\dsi\fastutil\7.1.0\fastutil-7.1.0.jar",
            @"org\apache\logging\log4j\log4j-api\2.8.1\log4j-api-2.8.1.jar",
            @"org\apache\logging\log4j\log4j-core\2.8.1\log4j-core-2.8.1.jar",
            @"org\lwjgl\lwjgl\lwjgl\2.9.4-nightly-20150209\lwjgl-2.9.4-nightly-20150209.jar",
            @"org\lwjgl\lwjgl\lwjgl_util\2.9.4-nightly-20150209\lwjgl_util-2.9.4-nightly-20150209.jar",
            @"com\mojang\text2speech\1.10.3\text2speech-1.10.3.jar"
        };

        string librariesDir = Path.Combine(mcRoot, "libraries");
        foreach (string relativePath in relativeJarPaths)
        {
            string fullPath = Path.Combine(librariesDir, relativePath);
            if (File.Exists(fullPath))
            {
                jars.Add(fullPath);
            }
            else
            {
                Console.WriteLine($"[警告] 缺失 jar: {fullPath}");
            }
        }

        string versionJar = Path.Combine(mcRoot, "versions", version, version + ".jar");
        if (File.Exists(versionJar))
        {
            jars.Add(versionJar);
        }
        else
        {
            Console.WriteLine($"[错误] 主 jar 不存在: {versionJar}");
        }

        return string.Join(";", jars);
    }
    string GetForgeOptiFineClassPath(string mcRoot, string version)
    {
        List<string> jars = new List<string>();

        string basePath = mcRoot.TrimEnd('\\', '/');
        string libs = Path.Combine(basePath, "libraries");
        string versionDir = Path.Combine(basePath, "versions", version);

        string[] jarPaths = new string[]
        {
        @"com\mojang\patchy\1.3.9\patchy-1.3.9.jar",
        @"oshi-project\oshi-core\1.1\oshi-core-1.1.jar",
        @"net\java\dev\jna\jna\4.4.0\jna-4.4.0.jar",
        @"net\java\dev\jna\platform\3.4.0\platform-3.4.0.jar",
        @"com\ibm\icu\icu4j-core-mojang\51.2\icu4j-core-mojang-51.2.jar",
        @"net\sf\jopt-simple\jopt-simple\5.0.3\jopt-simple-5.0.3.jar",
        @"com\paulscode\codecjorbis\20101023\codecjorbis-20101023.jar",
        @"com\paulscode\codecwav\20101023\codecwav-20101023.jar",
        @"com\paulscode\libraryjavasound\20101123\libraryjavasound-20101123.jar",
        @"com\paulscode\librarylwjglopenal\20100824\librarylwjglopenal-20100824.jar",
        @"com\paulscode\soundsystem\20120107\soundsystem-20120107.jar",
        @"io\netty\netty-all\4.1.9.Final\netty-all-4.1.9.Final.jar",
        @"com\google\guava\guava\21.0\guava-21.0.jar",
        @"org\apache\commons\commons-lang3\3.5\commons-lang3-3.5.jar",
        @"commons-io\commons-io\2.5\commons-io-2.5.jar",
        @"commons-codec\commons-codec\1.10\commons-codec-1.10.jar",
        @"net\java\jinput\jinput\2.0.5\jinput-2.0.5.jar",
        @"net\java\jutils\jutils\1.0.0\jutils-1.0.0.jar",
        @"com\google\code\gson\gson\2.8.0\gson-2.8.0.jar",
        @"com\mojang\authlib\1.5.25\authlib-1.5.25.jar",
        @"com\mojang\realms\1.10.22\realms-1.10.22.jar",
        @"org\apache\commons\commons-compress\1.8.1\commons-compress-1.8.1.jar",
        @"org\apache\httpcomponents\httpclient\4.3.3\httpclient-4.3.3.jar",
        @"commons-logging\commons-logging\1.1.3\commons-logging-1.1.3.jar",
        @"org\apache\httpcomponents\httpcore\4.3.2\httpcore-4.3.2.jar",
        @"it\unimi\dsi\fastutil\7.1.0\fastutil-7.1.0.jar",
        @"org\apache\logging\log4j\log4j-api\2.8.1\log4j-api-2.8.1.jar",
        @"org\apache\logging\log4j\log4j-core\2.8.1\log4j-core-2.8.1.jar",
        @"org\lwjgl\lwjgl\lwjgl\2.9.4-nightly-20150209\lwjgl-2.9.4-nightly-20150209.jar",
        @"org\lwjgl\lwjgl\lwjgl_util\2.9.4-nightly-20150209\lwjgl_util-2.9.4-nightly-20150209.jar",
        @"com\mojang\text2speech\1.10.3\text2speech-1.10.3.jar",
        @"net\minecraftforge\forge\1.12.2-14.23.5.2847\forge-1.12.2-14.23.5.2847.jar",
        @"net\minecraft\launchwrapper\1.12\launchwrapper-1.12.jar",
        @"org\ow2\asm\asm-all\5.2\asm-all-5.2.jar",
        @"org\jline\jline\3.5.1\jline-3.5.1.jar",
        @"com\typesafe\akka\akka-actor_2.11\2.3.3\akka-actor_2.11-2.3.3.jar",
        @"com\typesafe\config\1.2.1\config-1.2.1.jar",
        @"org\scala-lang\scala-actors-migration_2.11\1.1.0\scala-actors-migration_2.11-1.1.0.jar",
        @"org\scala-lang\scala-compiler\2.11.1\scala-compiler-2.11.1.jar",
        @"org\scala-lang\plugins\scala-continuations-library_2.11\1.0.2\scala-continuations-library_2.11-1.0.2.jar",
        @"org\scala-lang\plugins\scala-continuations-plugin_2.11.1\1.0.2\scala-continuations-plugin_2.11.1-1.0.2.jar",
        @"org\scala-lang\scala-library\2.11.1\scala-library-2.11.1.jar",
        @"org\scala-lang\scala-parser-combinators_2.11\1.0.1\scala-parser-combinators_2.11-1.0.1.jar",
        @"org\scala-lang\scala-reflect\2.11.1\scala-reflect-2.11.1.jar",
        @"org\scala-lang\scala-swing_2.11\1.0.1\scala-swing_2.11-1.0.1.jar",
        @"org\scala-lang\scala-xml_2.11\1.0.2\scala-xml_2.11-1.0.2.jar",
        @"lzma\lzma\0.0.1\lzma-0.0.1.jar",
        @"java3d\vecmath\1.5.2\vecmath-1.5.2.jar",
        @"net\sf\trove4j\trove4j\3.0.3\trove4j-3.0.3.jar",
        @"org\apache\maven\maven-artifact\3.5.3\maven-artifact-3.5.3.jar"
        };

        foreach (string relativePath in jarPaths)
        {
            string fullPath = Path.Combine(libs, relativePath);
            if (File.Exists(fullPath))
            {
                jars.Add(fullPath);
            }
        }

        string versionJar = Path.Combine(versionDir, version + ".jar");
        if (File.Exists(versionJar))
        {
            jars.Add(versionJar);
        }

        return string.Join(";", jars);
    }



}
