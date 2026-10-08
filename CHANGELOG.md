# Changelog

## [Unreleased]
### Breaking changes
- Object pools are typed.
- **One lifecycle: `Spawnable` has one `Spawn()` and one `Despawn()`, pooled or not.** `Poolable`, `PoolableComponent`, `PoolReference` and the old activation-driven `Spawnable` (it was in `SeweralIdeas.UnityUtils`) are replaced by `SeweralIdeas.ObjectPooling.Spawnable` (the root of an object with a life) and `SpawnablePart` (a part that its root spawns and despawns).
  - `Spawn()` activates the object and runs `OnSpawn` on the root, then on its parts. `Despawn()` ends the life at the end of the frame (like `Destroy`): `OnDespawn` runs on the parts last to first, then on the root, and the object goes back to its pool, or is destroyed if it has none. Destroying a spawned object despawns it. An object that nobody spawned and that isn't pooled spawns itself on its first Start.
  - Spawning no longer follows the object being enabled or disabled: a spawned object can be deactivated, put under an inactive parent or have components disabled, and stay spawned. Unity's own OnEnable and OnDisable are free for subclasses. For a new instance, `OnSpawn` runs after Awake and OnEnable but **before Start** (it used to come after Start).
  - `ObjectPool<T>.Take(active: true)` hands the instance out spawned; `Take(active: false)` hands it out un-spawned, to be set up and then spawned with `Spawn()` (it used to be `gameObject.SetActive(true)`, which now spawns nothing). `ObjectPool<T>.Return` and `Pool<T>.Return` are gone: despawn the instance. A prefab without a `Spawnable` gets a plain one when it is pooled.
  - Upgrade: `: Poolable` becomes `: Spawnable`, `: PoolableComponent` becomes `: SpawnablePart`, `Release()` becomes `Despawn()`, `ReleaseImmediate()` is gone, `Released` becomes `Despawned` (one life per subscription), `IsInPool` and `IsReleasing` become `IsInPool` and `IsDespawning`, `TryGetComponent<PoolReference>` becomes `IsPooled`, and `SetActive(true)` after `Take(active: false)` becomes `Spawn()`. `OnStart` stays, and the `Spawnable` has to be in `SeweralIdeas.ObjectPooling` (an assembly that uses it references it).
### Added
- `Take(position, rotation, active = true, parent = null)` on `ObjectPool<T>` and `Pool<T>`: the instance is placed (world space) before it is activated, so its OnEnable and Start already see it there.
- `DrawerChain` and `IChainedDrawer`: Unity runs only one property drawer per field, so a field with several drawer attributes had all but one ignored. A drawer that implements `IChainedDrawer` (and starts with `DrawerChain.Start(this)` in OnGUI and GetPropertyHeight) now runs the field's drawers in `order` order and passes control on to the next one. A drawer that isn't chained still works, but ends the chain. It finds the other drawers and fills in `PropertyDrawer`'s private `m_Attribute` and `m_FieldInfo` by reflection, so it may need attention when Unity changes those.
- `[Flatten]`: draws a field whose type has a single serialized field as that inner field, under the outer field's name (display only; the serialized data keeps the wrapper). Falls back to normal drawing if the type doesn't have exactly one visible field.
- `Spawnable.SpawnCancellationToken`: a token for one life of the object, cancelled when it despawns (or is destroyed), for async work that must not outlive the life. A new one for every life, made when first asked for, and disposed on despawn. It throws if the object isn't spawned. An exception from a callback on the token is logged and doesn't keep `OnDespawn` from running.

### Fixed
- `[ReadOnly]`, `[EditorOnly]`, `[PlayerOnly]` and `[Button]` no longer re-enable the GUI: they used to set `GUI.enabled` to `true` (or to their own value) afterwards, so a field inside an already disabled region, and everything after it, came back enabled. They now disable on top of the surrounding state.
- `[EnumFlag]` showed "mixed" whenever several objects were selected (it compared boxed enums by reference); it now uses the serialized property's own mixed-value state, and shows a message on a non-enum field instead of throwing.
- `[BitMask]` wrote the field on every repaint, so selecting several objects with different values changed them all to the first one's value. It now writes only when edited, and shows mixed values.
- `[InstantiateGUI]`: the type dropdown used a `SerializedProperty` after the `OnGUI` that made it was over; it now finds the property again by path, and gives every selected object its own instance.
- `[Angle]` set `GUI.matrix` and `GUI.color` to the defaults afterwards, not to what they were.

### Changed
- `[ReadOnly]`, `[EditorOnly]`, `[PlayerOnly]`, `[Color]`, `[Units]` and `[Button]` are chained drawers, so they combine with each other and with `[Condition]` and `[Flatten]` on one field.
- `ConditionDrawer` is chained: other drawer attributes on a `[Condition]` field (such as `[ReadOnly]` or `[Flatten]`) now apply, so long as they come after it in `order` (attributes of equal order keep declaration order).

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
