using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Services.MicroServices.PersistanceService
{
    public class LocalPersistenceService : IPersistenceService, IDisposable
    {
        private const int SETS_BEFORE_FLUSH = 1;

        private readonly LocalPersistenceSerializerSettings m_serializerSettings =
            new LocalPersistenceSerializerSettings();

        private Counter m_flushCounter;

        public void Initialize()
        {
            m_flushCounter = new Counter(SETS_BEFORE_FLUSH, Flush);
            Application.quitting += Flush;
        }

        private readonly Dictionary<string, IPersistentElement> m_loadedStateElements =
            new Dictionary<string, IPersistentElement>();

        private string GetSavePathForElement(string p_element) =>
            Path.Combine(Application.persistentDataPath, p_element + ".json");


        private string GetRoot(params string[] p_keys) => string.Join(".", p_keys);

        private string Serialize<T>(T p_toSerialize)
        {
            if (p_toSerialize is IPersistentElement l_persistentElement)
            {
                l_persistentElement.OnBeforeSerialize();
            }

            return JsonConvert.SerializeObject(p_toSerialize, m_serializerSettings);
        }

        private T Deserialize<T>(string p_toDeserialize)
        {
            var l_toReturn = JsonConvert.DeserializeObject<T>(p_toDeserialize, m_serializerSettings);
            return l_toReturn;
        }

        public T Get<T>(T p_toOverride, params string[] p_keys) where T : IPersistentElement
        {
            string l_root = GetRoot(p_keys);

            string l_jsonData = string.Empty;
            if (m_loadedStateElements.ContainsKey(l_root))
            {
                l_jsonData = Serialize(m_loadedStateElements[l_root]);
            }

            string l_filePath = GetSavePathForElement(l_root);
            if (string.IsNullOrEmpty(l_jsonData) && !File.Exists(l_filePath))
            {
                return p_toOverride;
            }

            l_jsonData = File.ReadAllText(GetSavePathForElement(l_root));
            JObject l_overrideData = Deserialize<JObject>(l_jsonData);
            JsonUtility.FromJsonOverwrite(l_overrideData.ToString(), p_toOverride);
            p_toOverride.OnAfterDeserialize();

            return p_toOverride;
        }

        public T Get<T>(params string[] p_keys) where T : IPersistentElement
        {
            string l_root = GetRoot(p_keys);

            if (m_loadedStateElements.ContainsKey(l_root))
            {
                return (T)m_loadedStateElements[l_root];
            }

            string l_elementFilePath = GetSavePathForElement(l_root);
            if (!File.Exists(l_elementFilePath))
            {
                return default(T);
            }

            try
            {
                string l_jsonData = File.ReadAllText(GetSavePathForElement(l_root));
                T l_elementToLoad = Deserialize<T>(l_jsonData);
                l_elementToLoad.OnAfterDeserialize();

                m_loadedStateElements.Add(l_root, l_elementToLoad);

                return l_elementToLoad;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Set<T>(T p_element, params string[] p_path) where T : IPersistentElement
        {
            string l_root = GetRoot(p_path);

            m_loadedStateElements[l_root] = p_element;

            m_flushCounter.Add();
        }

        public void Flush()
        {
            foreach (string l_element in m_loadedStateElements.Keys)
            {
                Flush(l_element);
            }
        }

        public void Flush(params string[] p_path)
        {
            string l_root = p_path.Length == 1 ? p_path[0] : GetRoot(p_path);
            string l_path = GetSavePathForElement(l_root);
            string l_directory = Path.GetDirectoryName(l_path);
            string l_state = Serialize(m_loadedStateElements[l_root]);

            if (!Directory.Exists(l_path))
            {
                Directory.CreateDirectory(l_directory);
            }

            StreamWriter l_writer = new StreamWriter(l_path);
            l_writer.Write(l_state);
            l_writer.Close();
        }

        private void ReleaseUnmanagedResources()
        {
            Application.quitting -= Flush;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~LocalPersistenceService()
        {
            ReleaseUnmanagedResources();
        }
    }
}