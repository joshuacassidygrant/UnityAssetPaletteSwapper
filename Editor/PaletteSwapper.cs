using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;


//Version 0.1
//19 May 2017
//by joshuacassidygrant

[System.Serializable]
public class ColourList: ScriptableObject {

    [SerializeField]
    public Color[] colours;



    public ColourList(Color[] _colours) {
        colours = _colours;
    }

}

public class PaletteSwapper : EditorWindow {

    Texture2D input_texture;
    Texture2D input_texture_cache;

    string input_directory;
    string replace_from;
    string replace_to;
    string[] loaded_asset_paths;
    List<Texture2D> loaded_texture_array;
    List<AnimationClip> loaded_anim_array;
    List<Animator> loaded_controller_array;
    

    string output_name;
    string destination_folder;


    bool batch;
    bool upload_processed;
    bool folder_loaded;
    bool colours_loaded;


    [SerializeField]
    SerializedObject input_colour_list;

    [SerializeField]
    SerializedObject replace_colour_list;


    [MenuItem("Tools/PaletteSwapper")]
    public static void ShowWindow() {
        GetWindow(typeof(PaletteSwapper));
    }
    
    void OnEnable() {
        upload_processed = false;

        if (input_colour_list == null) {
            input_colour_list = new SerializedObject(new ColourList(new Color[] { }));
            replace_colour_list = new SerializedObject(new ColourList(new Color[] { }));
        }
    }

    public PaletteSwapper() {
        this.titleContent = new GUIContent("Palette Swap!");
    }


    void OnGUI() {


        GUILayout.BeginVertical();

        batch = EditorGUILayout.Toggle("Batch?", batch);

        if (!batch) {

            //SINGLE PROCESSING
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Output File Name");
            output_name = EditorGUILayout.TextField(output_name, GUILayout.Width(144));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Folder: Assets/");
            destination_folder = EditorGUILayout.TextField(destination_folder, GUILayout.Width(144));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            input_texture = (Texture2D)EditorGUILayout.ObjectField("Texture", input_texture, typeof(Texture2D), true, GUILayout.Width(250));


            GUILayout.EndHorizontal();

            if (input_texture != null & input_texture != input_texture_cache) {
                //if (GUILayout.Button("GO")) {
                input_colour_list = new SerializedObject(new ColourList(GetColourListFromTexture(input_texture)));
                replace_colour_list = new SerializedObject(new ColourList(GetColourListFromTexture(input_texture)));
                input_texture_cache = input_texture;
                colours_loaded = true;
                upload_processed = true;

            }
        } else {
            //BATCH PROCESSING
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Input Directory");
            input_directory = EditorGUILayout.TextField(input_directory, GUILayout.Width(144));
            if (GUILayout.Button("Load")) {

                loaded_asset_paths = LoadDirectory(input_directory);
                loaded_texture_array = new List<Texture2D>();
                loaded_anim_array = new List<AnimationClip>();
                loaded_controller_array = new List<Animator>();

                foreach (string path in loaded_asset_paths) {
                    //TODO: When unity 5.5, use AssetDatabase.GetMainAssetTypeAtPath()
                    if (path.EndsWith(".png")) {
                        loaded_texture_array.Add(AssetDatabase.LoadMainAssetAtPath(path) as Texture2D);
                    } else if (path.EndsWith(".anim")) {
                        loaded_anim_array.Add(AssetDatabase.LoadMainAssetAtPath(path) as AnimationClip);
                    } else if (path.EndsWith(".controller")) {
                        loaded_controller_array.Add(AssetDatabase.LoadMainAssetAtPath(path) as Animator);
                    }
                }
                folder_loaded = true;
                colours_loaded = false;

                Debug.Log(loaded_anim_array.Count);

                //TODO -- parse through animations and anim controllers to swap out
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Replace:");
            replace_from = EditorGUILayout.TextField(replace_from, GUILayout.Width(72));
            GUILayout.Label("to:");
            replace_to = EditorGUILayout.TextField(replace_to, GUILayout.Width(72));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Folder: Assets/");
            destination_folder = EditorGUILayout.TextField(destination_folder, GUILayout.Width(144));
            GUILayout.EndHorizontal();

            if (folder_loaded && loaded_texture_array.Count > 0 && colours_loaded == false) {
                //if (GUILayout.Button("GO")) {
                input_colour_list = new SerializedObject(new ColourList(GetColourListFromTextures(loaded_texture_array.ToArray())));
                replace_colour_list = new SerializedObject(new ColourList(GetColourListFromTextures(loaded_texture_array.ToArray())));
                colours_loaded = true;
                upload_processed = true;

            }

        }
 

        if (colours_loaded) {

            GUILayout.BeginHorizontal();
            //Debug.Log(input_colour_list.FindProperty("colours").arraySize);
            input_colour_list.Update();
            EditorGUILayout.PropertyField(input_colour_list.FindProperty("colours"), true);
            input_colour_list.ApplyModifiedProperties();
            replace_colour_list.Update();
            EditorGUILayout.PropertyField(replace_colour_list.FindProperty("colours"), true);
            replace_colour_list.ApplyModifiedProperties();
            GUILayout.EndHorizontal();
        }

        if (input_texture == null & upload_processed == true) {
            upload_processed = false;
        }
        

        if (GUILayout.Button("Swap Me!")) {
            if (!batch) {
                ProcessTexture(input_texture, input_colour_list, replace_colour_list, output_name, destination_folder);
            } else {
                ProcessTextures(loaded_texture_array.ToArray(), input_colour_list, replace_colour_list, replace_from, replace_to, destination_folder);
            }
        }

        GUILayout.EndVertical();


      
    }

    void ProcessTextures(Texture2D[] _inputs, SerializedObject colour_original, SerializedObject colour_replace, string replace_from, string replace_to, string folder) {
        int count = 0;
        foreach (Texture2D input in _inputs) {
            string filename = input.name.Replace(replace_from, replace_to);
            ProcessTexture(input, colour_original, colour_replace, filename, folder);
            EditorUtility.DisplayProgressBar("Processing Sprites", "Please wait", (float)count/(float)_inputs.Length);
            count++;
        }
        EditorUtility.ClearProgressBar();
    }

    void ProcessTexture(Texture2D _input, SerializedObject colour_original, SerializedObject colour_replace, string filename, string folder) {

        SerializedProperty swap_ins = colour_replace.FindProperty("colours");
        SerializedProperty swap_outs = colour_original.FindProperty("colours");

        //SwapPalette.SwapEntry[] swaps = _swaps.

        Texture2D input = GetReadableTexture(_input);


        Texture2D output = new Texture2D(_input.width, _input.height);

        //THEN get the pixels and create a new array for the output;
        Color[] input_pixels = input.GetPixels();

        Color[] output_pixels = new Color[input_pixels.Length];

        Color swap_out = GetColourFromInts(255, 240, 36);
        Color swap_in = GetColourFromInts(255, 0, 77);
        int matches = 0;

        for (int i = 0; i < input_pixels.Length; i++) {

            bool dirty = false;

            for (int j = 0; j < swap_ins.arraySize; j++) {
                if (swap_outs.GetArrayElementAtIndex(j).colorValue == input_pixels[i]) {
                    matches++;
                    output_pixels[i] = swap_ins.GetArrayElementAtIndex(j).colorValue;
                    dirty = true;
                }
            }

            if (!dirty) {
                output_pixels[i] = input_pixels[i];

            }
            //DO PROCESSING HERE
            if (input_pixels[i] == swap_out) {

                output_pixels[i] = swap_in;
            } else {
            }
        }

        output.SetPixels(output_pixels);
        output.Apply();
        byte[] bytes = output.EncodeToPNG();

        //TODO - set import settings for project

        string directory_path = "Assets/" + folder;

        if (!Directory.Exists(directory_path)) {
            Directory.CreateDirectory(directory_path);
        }

        string path = "Assets/" + folder + "/" + filename + ".png";

        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);


        //string path = AssetDatabase.GetAssetPath(someTexture);
        TextureImporter A = (TextureImporter)AssetImporter.GetAtPath(path);
        A.isReadable = true;
        A.spritePixelsPerUnit = 16;
        A.mipmapEnabled = false;
        A.filterMode = FilterMode.Point;
        //A.textureFormat = TextureFormat.true
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

    }

    private Texture2D GetReadableTexture(Texture2D _input) {
        // Create a temporary RenderTexture of the same size as the texture
        RenderTexture tmp = RenderTexture.GetTemporary(
                            _input.width,
                            _input.height,
                            0,
                            RenderTextureFormat.Default,
                            RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(_input, tmp);

        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;

        //Create real input, which should be a readable version of the _input texture
        Texture2D input = new Texture2D(_input.width, _input.height);
        input.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        input.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
        return input;
    }

   string[] LoadDirectory(string dir) {
        if (AssetDatabase.IsValidFolder(dir)) {


            string[] asset_guids = AssetDatabase.FindAssets("", new string[] { dir });
            List<string> asset_paths = new List<string>();

            for (int i = 0; i < asset_guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(asset_guids[i]);
                if (AssetDatabase.IsMainAsset(AssetDatabase.LoadAssetAtPath(path, typeof(Object)))) {
                    asset_paths.Add(AssetDatabase.GUIDToAssetPath(asset_guids[i]));
                }
            }

            return asset_paths.ToArray();
        } else {
            Debug.Log("Not valid folder");
        }
        return null;
    }

    Color[] GetColourListFromTexture(Texture2D _input) {
        Texture2D input = GetReadableTexture(_input);

        List<Color> colours = new List<Color>();

        Color[] pixels = input.GetPixels();

        foreach (Color colour in pixels) {
            if (!colours.Contains(colour) && colour.a != 0) {
                colours.Add(colour);
            }
        }

        return colours.ToArray();
    }

    Color[] GetColourListFromTextures(Texture2D[] _inputs) {
        List<Color> colours = new List<Color>();

        foreach (Texture2D _input in _inputs) {
            Texture2D input = GetReadableTexture(_input);


            Color[] pixels = input.GetPixels();

            foreach (Color colour in pixels) {
                if (!colours.Contains(colour) && colour.a != 0) {
                    colours.Add(colour);
                }
            }
        }



        return colours.ToArray();
    }

    Color GetColourFromInts(int r, int g, int b) {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    void OutputColourSwatches(Color[] colours) {

        for (int i = 0; i < colours.Length; i++) {
            //GUI.color = colours[i];
            //Rect rect = GUILayoutUtility.GetRect(24, 24);
            EditorGUILayout.ColorField(colours[i]);

        }

    }

    void DebugDumpColourArray(Color[] ca) {
        string str = "";
        foreach (Color c in ca) {
            str += c.r + " " + c.b + " " + c.g + " " + c.a + "\n";
        }
        Debug.Log(str);
    }
}
