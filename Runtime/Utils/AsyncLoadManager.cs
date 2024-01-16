using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace SeweralIdeas.Utils
{
    public abstract class AsyncLoadManager<TKey, TVal> : IDisposable
    {
        private readonly Dictionary<TKey, Record> m_records = new();

        public enum LoadStatus
        {
            None,
            Loading,
            Ready,
            Failed
        }

        private class Record
        {
            public readonly TKey Key;
            public readonly HashSet<Request> Requests = new();
            public readonly AsyncLoadManager<TKey, TVal> Manager;

            public Record(TKey key, AsyncLoadManager<TKey, TVal> manager)
            {
                Key = key;
                Manager = manager;
            }

            public LoadStatus Status
            {
                get
                {
                    if(LoadTask == null)
                        return LoadStatus.None;
                    if(LoadTask.IsCompletedSuccessfully)
                    {
                        if(LoadTask.Result == null)
                            return LoadStatus.Failed;
                        return LoadStatus.Ready;
                    }
                    if(LoadTask.IsFaulted)
                        return LoadStatus.Failed;
                    else
                        return LoadStatus.Loading;
                }
            }
            
            public Task<TVal> LoadTask { get; set; }
        }

        public class Request : IDisposable
        {
            public TKey Key { get; private set; }
            private Record m_record;

            public LoadStatus Status => m_record.Status;
            public Task<TVal> LoadTask => m_record.LoadTask;

            public Request(TKey key, AsyncLoadManager<TKey, TVal> manager)
            {
                Key = key;

                if(!manager.m_records.TryGetValue(key, out m_record))
                {
                    m_record = new Record(key, manager);
                    manager.AddRecord(m_record); 
                }
                m_record.Requests.Add(this);
            }

            ~Request()
            {
                if(m_record != null)
                {
                    string log = $"{nameof(Record)} not disposed of properly!";
#if UNITY_5_3_OR_NEWER
                    Debug.Log(log);
#else
                    Console.WriteLine(log);
#endif
                    Dispose();
                }
            }

            public void Dispose()
            {
                if(m_record == null)
                    throw new ObjectDisposedException($"{nameof(Request)} already disposed");

                GC.SuppressFinalize(this);
                
                m_record.Requests.Remove(this);
                if(m_record.Requests.Count <= 0)
                    m_record.Manager.RemoveRecord(m_record);
                
                m_record = null;
                Key = default;
            }
        }

        private void AddRecord(Record record)
        {
            m_records.Add(record.Key, record);
            record.LoadTask = AsyncLoad(record.Key);
        }

        private async void RemoveRecord(Record record)
        {
            TVal asset;
            bool assetLoaded;
            
            try
            {
                asset = await record.LoadTask;
                assetLoaded = true;
            }
            catch(InvalidOperationException)
            {
                asset = default;
                assetLoaded = false;
            }
            if(record.Requests.Count <= 0)
            {
                m_records.Remove(record.Key);
                if(assetLoaded)
                    Unload(record.Key, asset);
            }
            
        }

        public void Dispose()
        {
        }

        public Request CreateRequest(TKey key) => new Request(key, this);

        protected abstract Task<TVal> AsyncLoad(TKey key);
        protected abstract void Unload(TKey key, TVal val);
    }
}
