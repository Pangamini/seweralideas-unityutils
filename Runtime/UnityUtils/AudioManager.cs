using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeweralIdeas.Collections;
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.Networking;

using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils
{
    public class AudioManager : AsyncLoadManager<string, AudioClip>
    {
        public static AudioManager Instance { get; } = new();

        private static readonly HashSet<string> s_fileExtensions = new(StringComparer.InvariantCultureIgnoreCase){ ".wav", ".mp3", ".ogg" };
        public static ReadonlySetView<string> FileExtensions => new(s_fileExtensions);

        protected override async Task<AudioClip> AsyncLoad(string filePath)
        {
            string path = System.IO.Path.Combine("file://", filePath);
            var handler = new DownloadHandlerAudioClip(path, AudioType.UNKNOWN);
            handler.compressed = true;
            var wr = new UnityWebRequest(path, "GET", handler, null);
            var asyncOp = wr.SendWebRequest();

            while(!asyncOp.isDone)
            {
                await Task.Yield();
            }

            if(!string.IsNullOrEmpty(handler.error))
            {
                throw new Exception($"Error loading audio {filePath} : {handler.error}");
            }
            else
            {
                var clip = handler.audioClip;
                clip.name = filePath;
                return clip;
            }
        }

        protected override void Unload(string key, AudioClip val)
        {
            // Debug.Log($"Destroyed audio {key}");
            Object.DestroyImmediate(val);
        }
    }
}
