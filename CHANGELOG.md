# Changelog

## [0.2.0]
The first entry since 0.1.4 (April 2023), so it covers a lot. Written from the commit history, grouped by area.

### Breaking changes
- `Reactive` is now `Observable`, and `MySet` is `ObservableSet`. `Observable` callbacks are `Action<new>`: `ObservableAction<old, new>` is gone, and unsubscribing no longer raises `Changed` (`UnsubscribeWithoutNotify` is public).
- `AdvPopup` is renamed `AdvancedDropdown`. `MasterSlave` is renamed `ParentChild`. The `UI_SSAA` shader is renamed `UI_Effects`.
- Tween is in its own assembly and no longer depends on the UnityUtils one; the tween components' namespaces were fixed.
- `HierarchyIcons` are enabled by an attribute, not an interface. `Detector` uses `OnTriggerStay`, as `OnTriggerExit` is unreliable.
- `AssetByNameLookup` is a class (it was a ScriptableObject), enumerates a list, and is sorted by GUID. The importer is generalized behind an interface.
- `MultiSet.GetCount` returns 0 for a missing key instead of throwing.
- Removed: `AudioManager`, `AccumulativeControl` (never finished), the dependency on `SeweralIdeas.Pooling` and the editor `StackPool` dependency.

### Added
- **Pooling:** `SeweralIdeas.ObjectPooling` (own assembly, `Runtime/ObjectPooling`).
  - `ObjectPool` (one per prefab; `Take`, `Take<T>`, `Return`, `Prewarm`, max size) and `ObjectPoolManager` (finds or creates itself per scene with `GetOrCreate`, keeps each prefab's pool, releases things at the end of the frame).
  - `Poolable` (the root of a pooled object: `Release()`, delayed to the end of the frame like `Object.Destroy`, `ReleaseImmediate()`, `Released`, `IsInPool`, `IsReleasing`) and `PoolableComponent`.
  - `PoolPrewarmer` fills a prefab's pool at Start, with the instances' Awake done.
  - New instances stay dormant until activated, and `Take(active: false)` hands out one that has had its Awake.
  - `Spawnable` (`OnSpawn` means "enabled and started") underlies it.
- **Observables and collections:** `ObservableDictionary` (`TryGetValue`, `VisitAll`), `IReadonlyObservable`, `SubscribeAndEnumerate` and `UnsubscribeAndEnumerate`, implicit conversion of observable collections to their readonly wrappers, equality operators for readonly views, `Bictionary` and `IBictionary`, `Pair` and `UnorderedPair`, `MultiDictionary` value comparer and an `Add` that reports whether the key was present, `SerializableDictionary` (with basic methods), `SerializableGuid`, `CollectionExtensions` (`Shuffle`, `PickRandomUnique`, XML docs), `BoundsCorners`, `CachedStringFormatter`, `ComparisonComparer`.
- **Asset lookups:** `AssetByNameLookup` (add, remove, clear, `IAssetByNameTable`, `AutoAssetByNameLookup` with an editor), `AssetReferenceComponent`, `AudioClipCollection`.
- **Physics:**
  - `PhysicsUtil2D`: `CheckGameObject`, `CastGameObject` (with filtering), overlap queries on colliders, `TryGetRandomPoint` and `TouchesCircle` on colliders, and `OverlapCollider` for `PolygonCollider2D` through a hidden scratch collider.
  - `PhysicsUtil`: more overlap methods, `CheckGameObject`, faster overloads, and buffers that grow when too small.
  - Box collider edge radius is honoured in overlap, check and cast queries (`OverlapRoundedBox`, `RoundedBoxCast`, `GetBoxEdgeRadius`); the random point and circle touch tests for boxes still ignore it.
  - `Detector`, `TriggerRelay2D`, `CollisionFX`.
- **Tween:** `Tween`, `TweenToggle`, `TweenVector3`, `TweenVolume`, `Tween.RectSize` and `RectPos`, `SetValue`, `Toggle()`, `EnsureInitialized()`, `OnTargetValueChanged`, an unscaled time option, a default execution order.
- **Async and scenes:** `AsyncExtensions` (`ValueTask.ForgetSafe`, `AwaitDestructionAsync`), `AsyncLoadManager`, `SceneLoader` and `AdditiveSceneLoader` (global reservations, observable load process), `SimplePrewarmScene`, `GameObjectUtils.InstantiateInactive`, `GameObject.IsPlaying()`.
- **UI:**
  - Layout groups with navigation (and an editor), `ScrollRectNavigator`, `SelectableGroup`, `EventSystemSelectionFallback`, `EventSystemSelectionTrigger`, `AspectRatioLock`, `RectTransformFollower` (with an unscaled time mode), `SafeAreaRect`, `SplineGraphic`, `SplineParticles`, `AnimationEventRelay`, `ClampScreenPointToRectTransform`, `ColorTransform`.
  - Shaders: UI supersampling (now `UI_Effects`, with dithering, an RGBA colour mask and multiplying by vertex alpha), plus shader utilities.
- **Markdown:** `MarkdownToTmp.Convert(markdown, styleSet)` turns Markdown into TextMeshPro rich text, with every element dressed in a TMP style; `MarkdownStyleSet` (ScriptableObject) names the style of each element (headings, paragraph, bold, italic, bold italic, strikethrough, inline code, link, quote, rule, list items per level) plus the bullets and number format; `MarkdownText` fills a `TMP_Text` from a `TextAsset`. Handles headings, paragraphs and hard breaks, emphasis, inline code, links, nested bullet and numbered lists, quotes and rules; images become their alt text, and tables, footnotes and HTML are not handled. Needs no Markdown library, so it works in builds.
- **Editor tools:** `[ComponentPicker]` drawer (a dropdown next to a Component field), `TypeReference` and its drawer, `InstantiateGUI`, `ObservableDrawer`, `EnumSwitch`, `TypeDropdown`, `DelayedTextField` (also a layout version), `GUITable`, `ErrorCheckTool` (can be disabled), `CursorEditor`, `ModalProgressWindow`, an async `PromptWindow`, `EditorPrefsSingleton`, `AnimationClipSampler`, a `PremultiplyAlpha` texture importer option, `SetSelectionDirty` and `SetSelectionParentAsset` tools, and a project setting to disable UnityUtils' global features.
- **Misc:** `AggregateControl<T>`, like `MultiControl` but every enabled request counts instead of the highest priority one: the value is the default value with each request folded in by a combine function given to the constructor (lowest value, product, sum, any, ...); `InvertibleBoolEvent` (a bool event with an "on value" and an "on inverted" list of callbacks), `ParentComponent` and `ChildComponent`, `GetComponentsInChildrenRecursively`, vector extensions, `Spawnable`, `AnimatorField` setters, software cursor mode, Unity 6.6 compatibility directives.

### Changed
- Singletons reset without a domain reload, and `SceneSingleton` avoids GC allocations in `GetInstance`.
- `Spawnable` uses `Behaviour.didStart` instead of a flag of its own.
- `MultiControl` uses `Observable` internally.
- `Condition` drawer, `CurveDrawer` (shows the current value, bounds check) and `TypeReferenceDrawer` (handles mixed values) were updated. The Comments inspector now works properly with prefabs.
- Nullable and serialization analyzer warnings were silenced.

### Fixed
- `AspectRatioLock` producing NaNs, `Detector` and `Tween` initialization order problems (including `Tween` when the GameObject is deactivated immediately), callbacks when clearing `ObservableSet` and `ObservableDictionary`, a default `ReadonlyDictView`, null references in `CursorEditor`, `HierarchyIcons`, `ReflectionUtility` and `TypeReferenceDrawer`, `SimpleSceneSingleton` being created in the wrong scene.

## [0.1.4]
### Fixed
- A nasty ugly bug in MultiControl that prevented Requests from being removed

## [0.1.3]
### Added
- Reactive struct (value wrapper)
- AnimatorField implicit operators to cast from string
- Readonly collection views (HashSet, Dictionary, Array, List)
- TextMeshPro link opener
- SceneReferenceDrawer
- AudioClipCollection

## [0.1.2]
### Added
- Curve drawers

## [0.1.1]
### Added
- More vector extensions

## [0.1.0]
### Added
- Various common usage stuff
