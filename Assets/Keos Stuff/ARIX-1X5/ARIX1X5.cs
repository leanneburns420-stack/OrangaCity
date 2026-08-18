#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

public class ARIX1X5 : EditorWindow
{
    private const string GitHubApiUrl = "https://api.github.com/repos/KeoLotso/ARIX-1X5/contents";
    private const string RawGitHubUrl = "https://raw.githubusercontent.com/KeoLotso/ARIX-1X5/main";
    private const string SplashImagePath = "Assets/Keos Stuff/Icons/Arix 1X5 Banner.png";
    private const string IntroAudioPath = "Assets/Keos Stuff/Icons/ARIX 1X5 Intro.mp3";
    private const string FilesFolder = "files";

    private Vector2 scrollPosition;
    private List<FolderInfo> folderInfoList = new List<FolderInfo>();
    private List<FolderInfo> filteredFolderList = new List<FolderInfo>();
    private FolderInfo selectedFolder;
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle descriptionStyle;
    private GUIStyle detailsBoxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle linkStyle;
    private GUIStyle searchBoxStyle;
    private Texture2D folderIcon;
    private Texture2D backgroundTexture;
    private Texture2D splashImage;
    private Texture2D blackTexture;
    private bool isLoading = true;
    private string errorMessage = "";
    private bool stylesInitialized = false;

    private bool showSplash = true;
    private float splashStartTime;
    private float splashDuration = 3.0f;
    private float fadeInDuration = 0.5f;
    private float fadeOutDuration = 0.7f;

    private AudioClip introAudio;
    private bool isAudioPlaying = false;

    private string searchText = "";
    private bool filterHasTutorial = false;
    private bool filterHasPackage = false;
    private bool filterHasScripts = false;
    private bool filterHasMusic = false;

    private Color selectedColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);
    private Color normalEntryColor = new Color(0.25f, 0.25f, 0.25f, 0.3f);
    private Color detailsBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private Color linkColor = new Color(0.4f, 0.6f, 1.0f);

    private Dictionary<string, string> _responseCache = new Dictionary<string, string>();
    private DateTime _lastRequestTime = DateTime.MinValue;
    private Queue<GitHubItem> _foldersToLoad = new Queue<GitHubItem>();
    private bool _isLoadingFolder = false;
    private string _currentLoadingFolder = "";
    private float _loadProgress = 0f;
    private const int RequestIntervalMs = 1000;
    private const int MaxConcurrentRequests = 2;
    private int _activeRequests = 0;
    private readonly object _requestLock = new object();
    private bool isInitialLoading = true;


    [MenuItem("Window/ARIX1X5 Browser")]
    public static void ShowWindow()
    {
        GetWindow<ARIX1X5>("ARIX1X5 Browser");
    }

    void OnEnable()
    {
        LoadFolderIcon();
        LoadSplashImage();
        LoadIntroAudio();
        CreateBlackTexture();

        splashStartTime = (float)EditorApplication.timeSinceStartup;
        showSplash = true;
        isAudioPlaying = false;
        FetchRepositoryContents();

        EditorApplication.update += Update;
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
        StopAudio();
    }

    private void LoadIntroAudio()
    {
        introAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(IntroAudioPath);
        if (introAudio == null)
        {
            Debug.LogWarning("ARIX1X5: Intro audio not found at path: " + IntroAudioPath);
        }
    }

    private void PlayIntroAudio()
    {
        if (introAudio != null && !isAudioPlaying)
        {
            AudioUtility.PlayClip(introAudio);
            isAudioPlaying = true;
        }
    }

    private void StopAudio()
    {
        if (isAudioPlaying)
        {
            AudioUtility.StopAllClips();
            isAudioPlaying = false;
        }
    }

    private void CreateBlackTexture()
    {
        blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();
    }

    private void Update()
    {
        if (showSplash)
        {
            float elapsedTime = (float)EditorApplication.timeSinceStartup - splashStartTime;

            if (elapsedTime > 0.1f && !isAudioPlaying)
            {
                PlayIntroAudio();
            }

            if (elapsedTime >= splashDuration)
            {
                showSplash = false;
                StopAudio();
                Repaint();
            }
            else
            {
                Repaint();
            }
        }
    }

    private void LoadSplashImage()
    {
        splashImage = AssetDatabase.LoadAssetAtPath<Texture2D>(SplashImagePath);
        if (splashImage == null)
        {
            Debug.LogWarning("ARIX1X5: Splash image not found at path: " + SplashImagePath);
        }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized)
            return;

        headerStyle = new GUIStyle();
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.margin = new RectOffset(10, 10, 10, 10);

        titleStyle = new GUIStyle();
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;

        descriptionStyle = new GUIStyle();
        descriptionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        descriptionStyle.fontSize = 11;
        descriptionStyle.wordWrap = true;

        detailsBoxStyle = new GUIStyle(EditorStyles.helpBox);
        detailsBoxStyle.normal.textColor = Color.white;
        detailsBoxStyle.padding = new RectOffset(15, 15, 15, 15);
        detailsBoxStyle.margin = new RectOffset(5, 5, 5, 5);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.padding = new RectOffset(10, 10, 5, 5);

        linkStyle = new GUIStyle(EditorStyles.label);
        linkStyle.normal.textColor = linkColor;
        linkStyle.active.textColor = Color.white;
        linkStyle.hover.textColor = Color.cyan;
        linkStyle.fontStyle = FontStyle.Bold;

        searchBoxStyle = new GUIStyle(EditorStyles.toolbarSearchField);
        searchBoxStyle.margin = new RectOffset(5, 5, 5, 5);
        searchBoxStyle.fixedHeight = 22;

        CreateBackgroundTexture();
        stylesInitialized = true;
    }

    private void LoadFolderIcon()
    {
        folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
    }

    private void CreateBackgroundTexture()
    {
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f));
        backgroundTexture.Apply();
    }

    async void FetchRepositoryContents()
    {
        isInitialLoading = true;
        folderInfoList.Clear();
        filteredFolderList.Clear();
        selectedFolder = null;
        errorMessage = "";
        _foldersToLoad.Clear();
        _isLoadingFolder = false;
        _currentLoadingFolder = "";
        _loadProgress = 0f;

        try
        {
            List<GitHubItem> rootItems = await GetRepositoryContents(GitHubApiUrl);
            GitHubItem filesItem = rootItems.FirstOrDefault(i => i.type == "dir" && i.name == FilesFolder);

            if (filesItem == null)
            {
                errorMessage = $"Could not find '{FilesFolder}' folder in repository";
                Debug.LogError(errorMessage);
                isInitialLoading = false;
                return;
            }

            List<GitHubItem> filesContents = await GetRepositoryContents(filesItem.url);
            List<GitHubItem> contentFolders = filesContents.Where(i => i.type == "dir").ToList();

            foreach (var folder in contentFolders)
            {
                _foldersToLoad.Enqueue(folder);
            }

            isInitialLoading = false;

            StartLoadingNextFolder();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
            Debug.LogError(errorMessage);
            isInitialLoading = false;
            Repaint();
        }
    }

    private async void StartLoadingNextFolder()
    {
        if (_foldersToLoad.Count == 0 || _isLoadingFolder)
        {
            _loadProgress = 1.0f;
            return;
        }

        _isLoadingFolder = true;
        var folder = _foldersToLoad.Dequeue();
        _currentLoadingFolder = folder.name;
        _loadProgress = 1.0f - ((float)_foldersToLoad.Count / (folderInfoList.Count + _foldersToLoad.Count + 1));

        try
        {
            string infoJsonUrl = $"{RawGitHubUrl}/{FilesFolder}/{folder.path.Substring(folder.path.LastIndexOf('/') + 1)}/info.json";
            string infoJson = await DownloadStringContent(infoJsonUrl);

            if (!string.IsNullOrEmpty(infoJson))
            {
                try
                {
                    FolderInfo folderInfo = ParseInfoJson(infoJson);
                    folderInfo.FolderPath = folder.path;
                    folderInfo.FolderName = folder.name;

                    List<GitHubItem> folderContents = await GetRepositoryContents(folder.url);
                    folderInfo.HasUnityPackage = folderContents.Any(i => i.name.EndsWith(".unitypackage"));
                    folderInfo.HasCSharpScripts = folderContents.Any(i => i.name.EndsWith(".cs"));
                    folderInfo.ContentItems = folderContents;
                    folderInfo.HasTutorial = !string.IsNullOrEmpty(folderInfo.Tutorial) && folderInfo.Tutorial != "/";

                    folderInfoList.Add(folderInfo);
                    filteredFolderList = new List<FolderInfo>(folderInfoList);

                    Repaint();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing info.json for {folder.name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading folder {folder.name}: {ex.Message}");
        }
        finally
        {
            _isLoadingFolder = false;

            if (_foldersToLoad.Count > 0)
            {
                StartLoadingNextFolder();
            }
            else
            {
                _loadProgress = 1.0f;
            }

            Repaint();
        }
    }

    private FolderInfo ParseInfoJson(string jsonString)
    {
        string fixedJson = jsonString
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        FolderInfo info = new FolderInfo();
        string pattern = @"(\w+):\s*(.*?)(?=;|$)";
        MatchCollection matches = Regex.Matches(fixedJson, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                string key = match.Groups[1].Value.Trim();
                string value = match.Groups[2].Value.Trim();

                switch (key)
                {
                    case "Name":
                        info.Name = value;
                        break;
                    case "Description":
                        info.Description = value;
                        break;
                    case "Requirements":
                        info.Requirements = value;
                        break;
                    case "Creators":
                        info.Creators = value;
                        break;
                    case "VersionCode":
                        info.VersionCode = value;
                        break;
                    case "Tutorial":
                        info.Tutorial = value;
                        break;
                }
            }
        }

        return info;
    }

    async Task<List<GitHubItem>> GetRepositoryContents(string url)
    {
        string json = await DownloadStringContent(url);
        return JsonConvert.DeserializeObject<List<GitHubItem>>(json);
    }

    async Task<string> DownloadStringContent(string url)
    {
        if (_responseCache.ContainsKey(url))
        {
            return _responseCache[url];
        }

        await WaitForRequestSlot();

        try
        {
            lock (_requestLock)
            {
                _activeRequests++;
                _lastRequestTime = DateTime.Now;
            }

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("User-Agent", "Unity GitHub Browser");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Delay(100);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error downloading {url}: {webRequest.error}");
                    return null;
                }

                string content = webRequest.downloadHandler.text;
                _responseCache[url] = content;
                return content;
            }
        }
        finally
        {
            lock (_requestLock)
            {
                _activeRequests--;
            }
        }
    }

    private async Task WaitForRequestSlot()
    {
        while (true)
        {
            bool canProceed = false;

            lock (_requestLock)
            {
                var timeSinceLastRequest = DateTime.Now - _lastRequestTime;

                if (_activeRequests < MaxConcurrentRequests && 
                    timeSinceLastRequest.TotalMilliseconds >= RequestIntervalMs)
                {
                    canProceed = true;
                }
            }

            if (canProceed)
                break;

            await Task.Delay(100);
        }
    }

    void OnGUI()
    {
        if (!stylesInitialized)
            InitializeStyles();

        if (showSplash && splashImage != null)
        {
            DrawSplashScreen();
            return;
        }

        DrawBackground();
        DrawHeader();

        if (isInitialLoading)
        {
            EditorGUILayout.LabelField("Loading repository contents...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        if (_isLoadingFolder || _foldersToLoad.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Loading {_currentLoadingFolder}...", GUILayout.Width(150));
            EditorGUILayout.Space();

            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, _loadProgress, $"{Math.Round(_loadProgress * 100)}%");

            EditorGUILayout.EndHorizontal();
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            if (GUILayout.Button("Retry", GUILayout.Height(30)))
            {
                FetchRepositoryContents();
            }
            return;
        }

        DrawSearchAndFilters();

        EditorGUILayout.BeginHorizontal();
        DrawFolderList();
        DrawFolderDetails();
        EditorGUILayout.EndHorizontal();
    }

    void DrawSplashScreen()
    {
        float elapsedTime = (float)EditorApplication.timeSinceStartup - splashStartTime;
        float alpha = 0f;

        GUI.DrawTexture(new Rect(0, 0, position.width, position.height), blackTexture, ScaleMode.StretchToFill);

        if (elapsedTime < fadeInDuration)
        {
            alpha = elapsedTime / fadeInDuration;
        }
        else if (elapsedTime < splashDuration - fadeOutDuration)
        {
            alpha = 1.0f;
        }
        else
        {
            alpha = 1.0f - ((elapsedTime - (splashDuration - fadeOutDuration)) / fadeOutDuration);
        }

        alpha = Mathf.Clamp01(alpha);

        float width = Mathf.Min(position.width * 0.8f, splashImage.width);
        float height = width * ((float)splashImage.height / splashImage.width);

        Rect imageRect = new Rect(
            (position.width - width) / 2,
            (position.height - height) / 2,
            width,
            height
        );

        Color oldColor = GUI.color;
        GUI.color = new Color(1, 1, 1, alpha);
        GUI.DrawTexture(imageRect, splashImage, ScaleMode.ScaleToFit);
        GUI.color = oldColor;
    }

    void DrawBackground()
    {
        GUI.DrawTexture(new Rect(0, 0, position.width, position.height), backgroundTexture, ScaleMode.StretchToFill);
    }

    void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Proj. ARIX-1X5", headerStyle);
        EditorGUILayout.EndVertical();
    }

    void DrawSearchAndFilters()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        string newSearchText = EditorGUILayout.TextField(searchText, searchBoxStyle);
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            ApplyFilters();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filters:", GUILayout.Width(50));

        bool newFilterHasTutorial = EditorGUILayout.ToggleLeft("Has Tutorial", filterHasTutorial, GUILayout.Width(100));
        if (newFilterHasTutorial != filterHasTutorial)
        {
            filterHasTutorial = newFilterHasTutorial;
            ApplyFilters();
        }

        bool newFilterHasPackage = EditorGUILayout.ToggleLeft("Has Package", filterHasPackage, GUILayout.Width(100));
        if (newFilterHasPackage != filterHasPackage)
        {
            filterHasPackage = newFilterHasPackage;
            ApplyFilters();
        }

        bool newFilterHasScripts = EditorGUILayout.ToggleLeft("Has Scripts", filterHasScripts, GUILayout.Width(100));
        if (newFilterHasScripts != filterHasScripts)
        {
            filterHasScripts = newFilterHasScripts;
            ApplyFilters();
        }

        bool newFilterHasMusic = EditorGUILayout.ToggleLeft("Has Music", filterHasMusic, GUILayout.Width(100));
        if (newFilterHasMusic != filterHasMusic)
        {
            filterHasMusic = newFilterHasMusic;
            ApplyFilters();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void ApplyFilters()
    {
        filteredFolderList = folderInfoList.Where(folder =>
        {
            bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                (folder.Name != null && folder.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (folder.Description != null && folder.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (folder.FolderName != null && folder.FolderName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesTutorial = !filterHasTutorial || folder.HasTutorial;
            bool matchesPackage = !filterHasPackage || folder.HasUnityPackage;
            bool matchesScripts = !filterHasScripts || folder.HasCSharpScripts;
            bool matchesMusic = !filterHasMusic || folder.ContentItems.Any(i =>
                i.name.EndsWith(".mp3") || i.name.EndsWith(".mva") || i.name.EndsWith(".wav") || i.name.EndsWith(".ogg"));

            return matchesSearch && matchesTutorial && matchesPackage && matchesScripts && matchesMusic;
        }).ToList();

        if (selectedFolder != null && !filteredFolderList.Contains(selectedFolder))
        {
            selectedFolder = null;
        }

        Repaint();
    }

    void DrawFolderList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Stuff there rn", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < filteredFolderList.Count; i++)
        {
            var folder = filteredFolderList[i];
            bool isSelected = selectedFolder == folder;
            Rect entryRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(50));

            if (isSelected)
            {
                EditorGUI.DrawRect(entryRect, selectedColor);
            }
            else if (i % 2 == 0)
            {
                EditorGUI.DrawRect(entryRect, normalEntryColor);
            }

            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(folderIcon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(folder.Name ?? folder.FolderName, titleStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(folder.Description ?? "No description", descriptionStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
            {
                selectedFolder = folder;
                Repaint();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawFolderDetails()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
        EditorGUILayout.Space(5);

        if (selectedFolder != null)
        {
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(detailsBoxStyle);

            GUILayout.Space(1);
            EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), detailsBackgroundColor);

            EditorGUILayout.LabelField("Name:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedFolder.Name ?? "Not specified");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedFolder.Description ?? "No description available", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Requirements:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedFolder.Requirements ?? "None specified", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Creators:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedFolder.Creators ?? "Unknown", EditorStyles.wordWrappedLabel);

            if (!string.IsNullOrEmpty(selectedFolder.VersionCode))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Version:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(selectedFolder.VersionCode, EditorStyles.wordWrappedLabel);
            }

            if (!string.IsNullOrEmpty(selectedFolder.Tutorial))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Tutorial:", EditorStyles.boldLabel);
                if (GUILayout.Button("Open Tutorial", linkStyle))
                {
                    Application.OpenURL(selectedFolder.Tutorial);
                }
            }

            EditorGUILayout.Space(10);

            // Unity Packages
            if (selectedFolder.HasUnityPackage)
            {
                var unityPackages = selectedFolder.ContentItems.Where(i => i.name.EndsWith(".unitypackage")).ToList();
                if (unityPackages.Count == 1)
                {
                    if (GUILayout.Button($"Import {unityPackages[0].name}", buttonStyle, GUILayout.Height(30)))
                    {
                        ImportUnityPackage(unityPackages[0]);
                    }
                }
                else if (unityPackages.Count > 1)
                {
                    EditorGUILayout.LabelField("Unity Packages:", EditorStyles.boldLabel);
                    string[] packageNames = unityPackages.Select(p => p.name).ToArray();
                    int selectedPackageIndex = 0;

                    selectedPackageIndex = EditorGUILayout.Popup("Select Package", selectedPackageIndex, packageNames);
                    if (GUILayout.Button("Import Selected Package", buttonStyle, GUILayout.Height(30)))
                    {
                        ImportUnityPackage(unityPackages[selectedPackageIndex]);
                    }
                }
            }

            // Scripts
            if (selectedFolder.HasCSharpScripts)
            {
                var scripts = selectedFolder.ContentItems.Where(i => i.name.EndsWith(".cs")).ToList();
                if (scripts.Count == 1)
                {
                    if (GUILayout.Button($"Import {scripts[0].name}", buttonStyle, GUILayout.Height(30)))
                    {
                        ImportSelectedScripts(scripts, new bool[] { true });
                    }
                }
                else if (scripts.Count > 1)
                {
                    EditorGUILayout.LabelField("C# Scripts:", EditorStyles.boldLabel);
                    string[] scriptNames = scripts.Select(s => s.name).ToArray();
                    bool[] selectedScripts = new bool[scripts.Count];

                    for (int i = 0; i < scripts.Count; i++)
                    {
                        selectedScripts[i] = EditorGUILayout.ToggleLeft(scriptNames[i], selectedScripts[i]);
                    }

                    if (GUILayout.Button("Import Selected Scripts", buttonStyle, GUILayout.Height(30)))
                    {
                        ImportSelectedScripts(scripts, selectedScripts);
                    }
                }
            }

            // Music Files
            var musicFiles = selectedFolder.ContentItems.Where(i =>
                i.name.EndsWith(".mp3") || i.name.EndsWith(".mva") || i.name.EndsWith(".wav") || i.name.EndsWith(".ogg")).ToList();

            if (musicFiles.Count == 1)
            {
                if (GUILayout.Button($"Download {musicFiles[0].name}", buttonStyle, GUILayout.Height(30)))
                {
                    DownloadSelectedMusicFiles(musicFiles, new bool[] { true });
                }
            }
            else if (musicFiles.Count > 1)
            {
                EditorGUILayout.LabelField("Music Files:", EditorStyles.boldLabel);
                string[] musicFileNames = musicFiles.Select(m => m.name).ToArray();
                bool[] selectedMusicFiles = new bool[musicFiles.Count];

                for (int i = 0; i < musicFiles.Count; i++)
                {
                    selectedMusicFiles[i] = EditorGUILayout.ToggleLeft(musicFileNames[i], selectedMusicFiles[i]);
                }

                if (GUILayout.Button("Download Selected Music Files", buttonStyle, GUILayout.Height(30)))
                {
                    DownloadSelectedMusicFiles(musicFiles, selectedMusicFiles);
                }
            }

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.LabelField("Select a folder to view details", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    async void ImportUnityPackage(GitHubItem unityPackage)
    {
        if (unityPackage == null) return;

        try
        {
            string targetDirectory = $"Assets/Keos Stuff/{selectedFolder.Name ?? selectedFolder.FolderName}";
            Directory.CreateDirectory(targetDirectory);

            string tempFilePath = Path.Combine(Path.GetTempPath(), unityPackage.name);

            EditorUtility.DisplayProgressBar("Downloading", $"Downloading {unityPackage.name}...", 0.5f);
            await DownloadFile(unityPackage.download_url, tempFilePath);

            EditorUtility.ClearProgressBar();
            AssetDatabase.ImportPackage(tempFilePath, true);

            EditorUtility.DisplayDialog("Import Complete",
                $"The package {unityPackage.name} has been imported to {targetDirectory}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Import Failed", $"Failed to import package: {ex.Message}", "OK");
            Debug.LogError($"Failed to import Unity package: {ex.Message}");
        }
    }

    async void ImportSelectedScripts(List<GitHubItem> scripts, bool[] selectedScripts)
    {
        if (scripts == null || selectedScripts == null) return;

        try
        {
            string targetDirectory = $"Assets/Keos Stuff/{selectedFolder.Name ?? selectedFolder.FolderName}";
            Directory.CreateDirectory(targetDirectory);

            EditorUtility.DisplayProgressBar("Importing", "Importing selected C# scripts...", 0.1f);

            for (int i = 0; i < scripts.Count; i++)
            {
                if (!selectedScripts[i]) continue;

                var script = scripts[i];
                float progress = (float)(i + 1) / scripts.Count;
                EditorUtility.DisplayProgressBar("Importing", $"Importing {script.name}...", progress);

                string scriptContent = await DownloadStringContent(script.download_url);
                string targetPath = Path.Combine(targetDirectory, script.name);
                File.WriteAllText(targetPath, scriptContent);
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Import Complete",
                $"Selected C# scripts have been imported to {targetDirectory}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Import Failed", $"Failed to import scripts: {ex.Message}", "OK");
            Debug.LogError($"Failed to import selected C# scripts: {ex.Message}");
        }
    }

    async void DownloadSelectedMusicFiles(List<GitHubItem> musicFiles, bool[] selectedMusicFiles)
    {
        if (musicFiles == null || selectedMusicFiles == null) return;

        try
        {
            string targetDirectory = "Assets/Keos Stuff/Music";
            Directory.CreateDirectory(targetDirectory);

            EditorUtility.DisplayProgressBar("Downloading", "Downloading selected music files...", 0.1f);

            for (int i = 0; i < musicFiles.Count; i++)
            {
                if (!selectedMusicFiles[i]) continue;

                var musicFile = musicFiles[i];
                float progress = (float)(i + 1) / musicFiles.Count;
                EditorUtility.DisplayProgressBar("Downloading", $"Downloading {musicFile.name}...", progress);

                string targetPath = Path.Combine(targetDirectory, musicFile.name);
                await DownloadFile(musicFile.download_url, targetPath);
            }

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Download Complete",
                $"Selected music files have been downloaded to {targetDirectory}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Download Failed", $"Failed to download music files: {ex.Message}", "OK");
            Debug.LogError($"Failed to download selected music files: {ex.Message}");
        }
    }

    async Task DownloadFile(string url, string targetPath)
    {
        await WaitForRequestSlot();

        try
        {
            lock (_requestLock)
            {
                _activeRequests++;
                _lastRequestTime = DateTime.Now;
            }

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("User-Agent", "Unity GitHub Browser");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Delay(100);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Download failed: {webRequest.error}");
                }

                File.WriteAllBytes(targetPath, webRequest.downloadHandler.data);
            }
        }
        finally
        {
            lock (_requestLock)
            {
                _activeRequests--;
            }
        }
    }

    public static class AudioUtility
    {
        private static AudioClip _currentClip;
        private static double _startTime;
        private static GameObject _tempGameObject;
        private static AudioSource _audioSource;

        public static void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            StopAllClips();

            _startTime = EditorApplication.timeSinceStartup;
            _currentClip = clip;

            _tempGameObject = new GameObject("EditorAudioPlayer");
            _tempGameObject.hideFlags = HideFlags.HideAndDontSave;
            _audioSource = _tempGameObject.AddComponent<AudioSource>();
            _audioSource.clip = clip;
            _audioSource.Play();

            EditorApplication.update += AudioUpdate;
        }

        private static void AudioUpdate()
        {
            if (_currentClip == null) return;

            double time = EditorApplication.timeSinceStartup;
            if (time > _startTime + _currentClip.length)
            {
                StopAllClips();
            }
        }

        public static void StopAllClips()
        {
            _currentClip = null;
            EditorApplication.update -= AudioUpdate;

            if (_tempGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_tempGameObject);
                _tempGameObject = null;
                _audioSource = null;
            }
        }
    }

    public class GitHubItem
    {
        public string name { get; set; }
        public string path { get; set; }
        public string url { get; set; }
        public string download_url { get; set; }
        public string type { get; set; }
    }

    public class FolderInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public string Creators { get; set; }
        public string VersionCode { get; set; }
        public string Tutorial { get; set; }
        public string FolderPath { get; set; }
        public string FolderName { get; set; }
        public bool HasUnityPackage { get; set; }
        public bool HasCSharpScripts { get; set; }
        public bool HasTutorial { get; set; }
        public List<GitHubItem> ContentItems { get; set; }
    }
}
#endif