using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class NebosPMsWindow : EditorWindow
{
    private const string GITHUB_API_BASE_URL = "https://api.github.com/repos/polardev-ui/NebosPlayerModels/contents/";
    private const string RAW_GITHUB_URL = "https://raw.githubusercontent.com/polardev-ui/NebosPlayerModels/main/";
    private const string LOCAL_ASSET_PATH = "Assets/Nebos Stuff/";

    private List<FolderInfo> folderInfos = new List<FolderInfo>();
    private Vector2 scrollPosition;
    private FolderInfo selectedFolder;
    private string creditsText = "";
    private bool showCredits = false;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle descriptionStyle;
    private GUIStyle buttonStyle;
    private bool isLoading = true;
    private Texture2D loadingIcon;
    private float loadingRotation = 0f;

    private const int GRID_ITEM_SIZE = 180;
    private const int GRID_SPACING = 10;
    private int gridItemsPerRow = 3;

    [MenuItem("Keos Stuff/Nebos Playermodels")]
    public static void ShowWindow()
    {
        var window = GetWindow<NebosPMsWindow>("Nebos Asset Browser");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        loadingIcon = EditorGUIUtility.FindTexture("d_Loading");
        EditorApplication.update += UpdateLoading;
        FetchRepositoryData();
        FetchCredits();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateLoading;
    }

    private void UpdateLoading()
    {
        loadingRotation += 2f;
        if (loadingRotation >= 360f)
            loadingRotation = 0f;

        Repaint();
    }

    private void InitializeStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.margin = new RectOffset(0, 0, 10, 15);
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(0, 0, 5, 5);
        }

        if (descriptionStyle == null)
        {
            descriptionStyle = new GUIStyle(EditorStyles.label);
            descriptionStyle.wordWrap = true;
            descriptionStyle.richText = true;
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.margin = new RectOffset(5, 5, 5, 5);
            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
            buttonStyle.fixedHeight = 0;
        }
    }

    private void OnGUI()
    {
        InitializeStyles();
        gridItemsPerRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 20) / (GRID_ITEM_SIZE + GRID_SPACING)));

        EditorGUILayout.BeginVertical();

        GUILayout.Label("Nebos Player Models Browser", titleStyle);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Show License", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            showCredits = !showCredits;
        }
        EditorGUILayout.EndHorizontal();

        if (showCredits)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CREDITS", headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.Height(200));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField(creditsText, descriptionStyle);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                showCredits = false;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (isLoading)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var rect = GUILayoutUtility.GetRect(50, 50);
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(loadingRotation, new Vector2(rect.center.x, rect.center.y));
            GUI.DrawTexture(rect, loadingIcon);
            GUI.matrix = matrix;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Loading repository data...", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }
        else if (selectedFolder == null)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int count = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var folderInfo in folderInfos)
            {
                if (count % gridItemsPerRow == 0 && count > 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(GRID_ITEM_SIZE), GUILayout.Height(GRID_ITEM_SIZE + 30));

                Rect imageRect = GUILayoutUtility.GetRect(GRID_ITEM_SIZE - 10, GRID_ITEM_SIZE - 40);
                if (folderInfo.coverTexture != null)
                {
                    GUI.DrawTexture(imageRect, folderInfo.coverTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(imageRect, Color.gray);
                }

                if (GUILayout.Button(folderInfo.folderName, GUILayout.Height(30)))
                {
                    selectedFolder = folderInfo;
                }

                EditorGUILayout.EndVertical();

                count++;
            }

            for (int i = 0; i < gridItemsPerRow - (count % gridItemsPerRow) && count % gridItemsPerRow != 0; i++)
            {
                GUILayout.Box("", GUIStyle.none, GUILayout.Width(GRID_ITEM_SIZE), GUILayout.Height(GRID_ITEM_SIZE + 30));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back to Grid", GUILayout.Height(30), GUILayout.Width(120)))
            {
                selectedFolder = null;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            GUILayout.Label(selectedFolder.folderName, titleStyle);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (selectedFolder.coverTexture != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(0, 300, GUILayout.ExpandWidth(true));
                float aspectRatio = (float)selectedFolder.coverTexture.width / selectedFolder.coverTexture.height;
                float width = Mathf.Min(previewRect.width, previewRect.height * aspectRatio);
                float height = width / aspectRatio;

                Rect centeredRect = new Rect(
                    previewRect.x + (previewRect.width - width) * 0.5f,
                    previewRect.y,
                    width,
                    height
                );

                GUI.DrawTexture(centeredRect, selectedFolder.coverTexture, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Description", headerStyle);
            EditorGUILayout.LabelField(selectedFolder.description, descriptionStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Download & Import All", buttonStyle, GUILayout.Height(40), GUILayout.Width(200)))
            {
                DownloadAssets(selectedFolder);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private async void FetchRepositoryData()
    {
        isLoading = true;
        folderInfos.Clear();

        try
        {
            string jsonData = await DownloadTextFile(GITHUB_API_BASE_URL);
            var rootItems = JsonUtility.FromJson<GitHubResponse>("{\"items\":" + jsonData + "}").items;

            var directories = rootItems.Where(item => item.type == "dir").ToList();

            foreach (var dir in directories)
            {
                string dirContentsJson = await DownloadTextFile(GITHUB_API_BASE_URL + dir.name);
                var dirItems = JsonUtility.FromJson<GitHubResponse>("{\"items\":" + dirContentsJson + "}").items;

                FolderInfo folderInfo = new FolderInfo
                {
                    folderName = dir.name,
                    files = dirItems.Select(item => new FileInfo { name = item.name, downloadUrl = item.download_url, path = item.path }).ToList()
                };

                var infoFile = dirItems.FirstOrDefault(item => item.name == "info.txt");
                if (infoFile != null && !string.IsNullOrEmpty(infoFile.download_url))
                {
                    folderInfo.description = await DownloadTextFile(infoFile.download_url);
                }
                else
                {
                    folderInfo.description = "No description available.";
                }

                var coverFile = dirItems.FirstOrDefault(item => item.name == "Cover.png");
                if (coverFile != null && !string.IsNullOrEmpty(coverFile.download_url))
                {
                    folderInfo.coverTexture = await DownloadTexture(coverFile.download_url);
                }

                folderInfos.Add(folderInfo);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error fetching repository data: " + e.Message);
        }

        isLoading = false;
    }

    private async void FetchCredits()
    {
        try
        {
            creditsText = await DownloadTextFile(RAW_GITHUB_URL + "CREDITS.md");
        }
        catch (Exception e)
        {
            creditsText = "Failed to load credits: " + e.Message;
            Debug.LogError("Error fetching credits: " + e.Message);
        }
    }

    private async Task<string> DownloadTextFile(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("User-Agent", "Unity Editor Script");
            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Delay(100);

            if (www.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(www.error);
            }

            return www.downloadHandler.text;
        }
    }

    private async Task<Texture2D> DownloadTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            www.SetRequestHeader("User-Agent", "Unity Editor Script");
            var operation = www.SendWebRequest();

            while (!operation.isDone)
                await Task.Delay(100);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download texture: {www.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(www);
        }
    }

    private async void DownloadAssets(FolderInfo folder)
    {
        string destinationFolder = Path.Combine(LOCAL_ASSET_PATH, folder.folderName);
        if (!Directory.Exists(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + destinationFolder))
        {
            Directory.CreateDirectory(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + destinationFolder);
        }

        int downloadCount = 0;
        int totalFiles = folder.files.Count(f => f.name != "info.txt" && f.name != "Cover.png");

        EditorUtility.DisplayProgressBar("Downloading Assets", "Preparing to download...", 0);

        try
        {
            foreach (var file in folder.files)
            {
                if (file.name == "info.txt" || file.name == "Cover.png")
                    continue;

                string fileName = file.name;
                string filePath = Path.Combine(destinationFolder, fileName);
                string fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + filePath;

                EditorUtility.DisplayProgressBar("Downloading Assets",
                    $"Downloading {fileName}... ({downloadCount}/{totalFiles})",
                    (float)downloadCount / totalFiles);

                using (UnityWebRequest www = UnityWebRequest.Get(file.downloadUrl))
                {
                    www.SetRequestHeader("User-Agent", "Unity Editor Script");
                    var operation = www.SendWebRequest();

                    while (!operation.isDone)
                        await Task.Delay(100);

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to download {fileName}: {www.error}");
                        continue;
                    }

                    File.WriteAllBytes(fullPath, www.downloadHandler.data);
                }

                downloadCount++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Download Complete",
                $"Successfully downloaded {downloadCount} files to {destinationFolder}", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Error downloading assets: " + e.Message);
            EditorUtility.DisplayDialog("Download Failed",
                "An error occurred while downloading assets. Check console for details.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [Serializable]
    private class GitHubItem
    {
        public string name;
        public string path;
        public string type;
        public string download_url;
    }

    [Serializable]
    private class GitHubResponse
    {
        public List<GitHubItem> items;
    }

    private class FolderInfo
    {
        public string folderName;
        public List<FileInfo> files = new List<FileInfo>();
        public string description;
        public Texture2D coverTexture;
    }

    private class FileInfo
    {
        public string name;
        public string downloadUrl;
        public string path;
    }
}