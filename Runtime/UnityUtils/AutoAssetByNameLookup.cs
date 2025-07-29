using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils
{
    public interface IAutoAssetByNameLookup
    {
        SearchMode Mode { get; }
        public Type GetElementType();
        
        public enum SearchMode
        {
            None = 0,
            Folder = 1,
            FolderAndSubfolders = 2,
            Project = 3
        }

    }
    
    public class AutoAssetByNameLookup<T> : AssetByNameLookup<T>, IAutoAssetByNameLookup where T : Object
    {
        [SerializeField] private IAutoAssetByNameLookup.SearchMode _searchMode;

        public IAutoAssetByNameLookup.SearchMode Mode => _searchMode;
        public Type GetElementType() => typeof(T);
    }
}
