using Newtonsoft.Json;

namespace TorneioBot
{
    public class LanguageManager
    {
        private Dictionary<string, Dictionary<string, string>> _languageStrings;
        private string _languageCode;
        public LanguageManager(string languageCode)
        {
            _languageCode = languageCode;
            _languageStrings = new Dictionary<string, Dictionary<string, string>>();            
            LoadLanguageFile("./languages/strings_pt.json");
            LoadLanguageFile("./languages/strings_en.json");            
        }

        private void LoadLanguageFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string fileContents = File.ReadAllText(filePath);
                Dictionary<string, string> languageStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);                
                string languageCode = Path.GetFileNameWithoutExtension(filePath).Split('_')[1];                
                _languageStrings[languageCode] = languageStrings;
            }
            else
            {                
                Console.WriteLine($"Arquivo de idioma não encontrado: {filePath}");
            }
        }

        public string GetLocalizedString(string key)
        {            
            if (_languageStrings.ContainsKey(_languageCode) && _languageStrings[_languageCode].ContainsKey(key))
            {
                return _languageStrings[_languageCode][key];
            }            
            return $"[{key}]";
        }
    }
    
}