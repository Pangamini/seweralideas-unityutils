# Changelog

## [Unreleased]
### Breaking changes
- `TweenComponent.Tween` is now `TweenComponent.Source`, of the new type `TweenBase` (`Tween` derives from it, with the same serialized data and script). `TweenComponent` requires a `TweenBase` instead of a `Tween`; `Tween.ValueChanged`, `Progress` and `ExecOrder` moved to `TweenBase`. Upgrade: `Tween.Tween` becomes `Source` (cast to `Tween` for `IsOn`).
- Object pools are typed.
- **One lifecycle: `Spawnable` has one `Spawn()` and one `Despawn()`, pooled or not.** `Poolable`, `PoolableComponent`, `PoolReference` and the old activation-driven `Spawnable` (it was in `SeweralIdeas.UnityUtils`) are replaced by `SeweralIdeas.ObjectPooling.Spawnable` (the root of an object with a life) and `SpawnablePart` (a part that its root spawns and despawns).
  - `Spawn()` activates the object and runs `OnSpawn` on the root, then on its parts. `Despawn()` ends the life at the end of the frame (like `Destroy`): `OnDespawn` runs on the parts last to first, then on the root, and the object goes back to its pool, or is destroyed if it has none. Destroying a spawned object despawns it. An object that nobody spawned and that isn't pooled spawns itself on its first Start.
  - Spawning no longer follows the object being enabled or disabled: a spawned object can be deactivated, put under an inactive parent or have components disabled, and stay spawned. Unity's own OnEnable and OnDisable are free for subclasses. For a new instance, `OnSpawn` runs after Awake and OnEnable but **before Start** (it used to come after Start).
  - `ObjectPool<T>.Take(active: true)` hands the instance out spawned; `Take(active: false)` hands it out un-spawned, to be set up and then spawned with `Spawn()` (it used to be `gameObject.SetActive(true)`, which now spawns nothing). `ObjectPool<T>.Return` and `Pool<T>.Return` are gone: despawn the instance. A prefab without a `Spawnable` gets a plain one when it is pooled.
  - Upgrade: `: Poolable` becomes `: Spawnable`, `: PoolableComponent` becomes `: SpawnablePart`, `Release()` becomes `Despawn()`, `ReleaseImmediate()` is gone, `Released` becomes `Despawned` (one life per subscription), `IsInPool` and `IsReleasing` become `IsInPool` and `IsDespawning`, `TryGetComponent<PoolReference>` becomes `IsPooled`, and `SetActive(true)` after `Take(active: false)` becomes `Spawn()`. `OnStart` stays, and the `Spawnable` has to be in `SeweralIdeas.ObjectPooling` (an assembly that uses it references it).
- `PromptWindow` validators return an error message (`Func<T, string?>`, null when valid) instead of a `bool`, and the window shows the message above the buttons while OK is disabled. Upgrade: `x => IsValid(x)` becomes `x => IsValid(x) ? null : "why not"`.
### Added
- `MarkdownToTmp.Convert(markdown, styles, keepTags)`: with `keepTags`, the TMP rich text tags in the text (`<b>`, `<style=Name>`, `<sprite ...>`, everything in `<noparse>`) are left as they are instead of being escaped, for text that has tags made by code next to its Markdown. A line that starts with a tag is not read as a heading or list item, and an emphasis next to one may not be recognised. It is off by default, so Markdown from elsewhere still cannot make tags.
- `ConfigLocaleSelector` (`SeweralIdeas.LocalizationExtensions`, `Runtime/Localization`): keeps the selected locale and a `StringConfigField` holding its code in sync. As a startup locale selector (add it first to the Startup Locale Selectors in the Localization settings and assign the field) it picks the stored locale, with no flash of the system language; after that a change of the field switches the locale and a change of the locale is written to the field. The assembly is only compiled with `com.unity.localization` (version define, like `AddressableExtensions`), but it references `SeweralIdeas.Config`, which has to be in the project whenever the Localization package is - a project with Localization and without Config will not compile this assembly. Initialization has to run for it to hook up (anything that touches `LocalizationSettings`).
- `ReplayableAnimation` (replayable effects): an effect component that plays the clip of the `AnimationClipSampler` next to it, from the start when the effect plays, at `Speed` and optionally `Loop`ing. It samples the clip, so `FastForward` lands exactly where the clip would be; without `Loop` it holds the last pose when the clip is over.
- `Or(fallback)` and `OrNull()` extensions on `UnityEngine.Object`: `Or` is a null coalescing that treats a destroyed object as null, which `??` does not; `OrNull()` turns a destroyed object into a real null so `?.` works after it (`target.OrNull()?.Foo()`). `Override<T>` is unaffected (`ValueOr` never null-checks the value).
- `Override<T>` (`SeweralIdeas.Utils`): a serialized value that may or may not override something else, like the fields of a Volume. A struct of a bool and a `T` (any type; a reference type can be overridden with null, which is not the same as no override), with no default of its own: `HasValue`, `Value` (throws without an override), `TryGetValue` and `ValueOr(fallback)` leave it to the reader to decide what applies without one. Converts implicitly from `T` (as an enabled override); an explicit cast to `T` throws without an override, like `Value`. For a value type, `AsNullable()` gives a `T?` that is null without an override. Drawn as one line, a toggle in the label column and the value's own control, greyed out while the toggle is off; made for single-line values.
- **`TweenBase` and `TweenImpulse`.** `TweenBase` is the source of the 0..1 value that `TweenComponent`s follow (`Progress`, `ValueChanged`, `EnsureInitialized()`), with one per object. `Tween` (on/off, with `IsOn`, `Toggle()` and `SetProgress()`) is one implementation; `TweenImpulse` is the new other one: `Trigger(intensity)` raises it (never lowers a stronger, still decaying one), then it falls linearly at `Decay Rate` per second, smoothed by `Smooth Time`, and it turns itself off when it has settled. Any tween component (`TweenVector3`, `TweenColor`, ...) works on it, e.g. a flash or a camera shake.
- `PlatformListener`: calls a `UnityEvent<bool>` once, on Start, with whether the platform the game runs on is in its `EnumSet<RuntimePlatform>` (or, with `Invert`, is not), e.g. to hide a Quit button on WebGL. `IsCurrentPlatformIncluded` reads the result. In the Editor the platform is the editor one (`WindowsEditor`, ...), not the player's.
- `EnumSet<T>`: a serialized set of enum values, drawn as one multi-select dropdown (a check per value in a menu; the field's label lists the chosen ones) instead of an array. It works with any enum, not only flags enums. Obsolete members are left out of the menu, and a value shared by several names is listed once. `Contains(value)` and `Values` read it. A `[PropertyAttribute]` on a `T[]` applies to every element, so this had to be a type of its own, not an attribute.
- **`HorizontalLayoutGroupWithNavigation` and `VerticalLayoutGroupWithNavigation` can take the navigation across their direction from another selectable** (the new `Inherit From` field): the elements lead where that selectable leads, Up and Down for the horizontal group, Left and Right for the vertical one. It is what the selectable finds, so it works with explicit navigation and the automatic one alike (`FindSelectableOnUp` and the like); a target that is an element of the group itself is ignored. With `Inherit From` set, the `Navigate ...` fields are not used (the inspector hides them, by a `[Condition]`); nothing changes when it is empty, which is the default of every existing group. The group watches the selectable in `LateUpdate` (only while it has one) and sets the navigation again when what it leads to changed, so it follows another layout group that navigates it later or at another time.
- **Replayable effects moved here** from the `seweralideas-replayableeffects` repository (`Runtime/ReplayableEffects`, `Editor/ReplayableEffects`; same namespace, assembly names and script GUIDs, no history). `ReplayableEffect` is a `Spawnable` (it was a `Poolable`): a pooled instance despawns itself when its duration is up, and `Stop()` ends it early (a `Despawn()` for a pooled instance, nothing for one that isn't). `Spawn(scene, ...)` places the instance before it is spawned. Upgrade: remove the old submodule, or its scripts exist twice.
- `Take(position, rotation, active = true, parent = null)` on `ObjectPool<T>` and `Pool<T>`: the instance is placed (world space) before it is activated, so its OnEnable and Start already see it there.
- `DrawerChain` and `IChainedDrawer`: Unity runs only one property drawer per field, so a field with several drawer attributes had all but one ignored. A drawer that implements `IChainedDrawer` (and starts with `DrawerChain.Start(this)` in OnGUI and GetPropertyHeight) now runs the field's drawers in `order` order and passes control on to the next one. A drawer that isn't chained still works, but ends the chain. It finds the other drawers and fills in `PropertyDrawer`'s private `m_Attribute` and `m_FieldInfo` by reflection, so it may need attention when Unity changes those.
- `[Flatten]`: draws a field whose type has a single serialized field as that inner field, under the outer field's name (display only; the serialized data keeps the wrapper). Falls back to normal drawing if the type doesn't have exactly one visible field.
- `Spawnable.SpawnCancellationToken`: a token for one life of the object, cancelled when it despawns (or is destroyed), for async work that must not outlive the life. A new one for every life, made when first asked for, and disposed on despawn. Once the life is over (the object is in its pool, or destroyed) it is an already cancelled token, so it behaves the same pooled or not. Asked for before the object is spawned, it is for the coming life (like `AwaitDespawnAsync`), so it no longer throws. An exception from a callback on the token is logged and doesn't keep `OnDespawn` from running.
- `Spawnable.GetLifeToken(MonoBehaviour)` and `Spawnable.AwaitLifeEndAsync(MonoBehaviour, ct)`: the end of the life of any behaviour, whether it has a `Spawnable` (its `SpawnCancellationToken` / `AwaitDespawnAsync`) or not (its `destroyCancellationToken` / destruction). A destroyed target gives a cancelled token / completes at once.
- A pool whose prefab is an object of a scene is destroyed when the prefab is (with the instances waiting in it); the instances handed out are destroyed instead of returned when they despawn. `ObjectPoolManager` drops the entries of such pools.
- `Spawnable.AwaitDespawnAsync(ct)`: completes when the life of the object ends (also when it is destroyed), so a pooled object, which is returned rather than destroyed, can be awaited like an unpooled one. Complete at once for an object in its pool; for one not spawned yet it waits for the coming life.

### Fixed
- **`SpawnablePart` without a `Spawnable` above it no longer fails.** It used to log an error and skip `OnAwake` (while `OnDestroyed` still ran, on state `OnAwake` had never set up). Now it is its own root: `OnAwake` runs, it is spawned on its first Start (as an unpooled `Spawnable` nobody spawned is) and despawned when destroyed. `SpawnablePart.Root` is nullable accordingly. `SpawnablePart` now defines `Start` (protected, like `Awake`), so a subclass must not declare its own.
- IL2CPP builds failed on `CollectionExtensions.PartialShuffle` ("No support for assigning to/from variable size types"). The `Shuffle` and `PartialShuffle` overloads generic over the list type (`<T, TList>`) now take an `IList<T>`, and `T` is inferred from the call, which the old form couldn't do. Upgrade: a call with explicit type arguments, `Shuffle<T, TList>()`, becomes `Shuffle()`.
- **Premultiply Alpha (texture importer) multiplied the sRGB-encoded color by the alpha.** The GPU decodes the color of an sRGB texture but not the alpha, so every translucent pixel ended up as `color * alpha^2.2` instead of `color * alpha` and was too dark (dark fringes around sprites, dark bands in gradients). The color is now decoded to linear, multiplied and encoded again; a texture imported with sRGB off is multiplied as before. Upgrade: reimport the textures that have "Premultiply Alpha" on, their pixels change (translucent areas get brighter).
- The delayed text fields (`SeweralGUI.DelayedTextField` / `DelayedTextArea`) showed a committed value (Enter) or released focus only once the mouse moved: nothing repainted the window after the key press, and `GUI.FocusControl(null)` takes effect on the next event. They release the keyboard focus at once and request a repaint when they commit, cancel or change focus.
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
