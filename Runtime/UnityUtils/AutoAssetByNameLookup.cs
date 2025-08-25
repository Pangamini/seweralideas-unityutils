using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils
{
    public interface IAutoFindAssets
    {
        SearchMode Mode { get; }
        Type GetElementType();

        string GetAssetArrayPath();
        
        public enum SearchMode
        {
            None = 0,
            Folder = 1,
            FolderAndSubfolders = 2,
            Project = 3
        }

    }
    
    public class AutoAssetByNameLookup<T> : AssetByNameLookup<T>, IAutoFindAssets where T : Object
    {
        [SerializeField] private IAutoFindAssets.SearchMode _searchMode;

        public IAutoFindAssets.SearchMode Mode => _searchMode;
        public Type GetElementType() => typeof(T);

        string IAutoFindAssets.GetAssetArrayPath() => "_table._list";
    }
}
