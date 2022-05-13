# OCB XML Core Mod - 7 Days to Die (A20) Addon

Core Mod enabling other mods to use a few advanced
or new (conditional) XML patching functionalities.

Note that this is currently in a very early stage and
you should probably not yet copy the code as advised,
until it gets more stable and hardened.

## Including other files

On the root level of each config xml you can define
includes that will load additional files. This is
nice for organizing larger mods. Includes are loaded
relative to the previously loaded file. But make
sure you don't recursively include stuff ;)

```xml
<include path="blocks.plants.xml"/>
```

## Conditional XML patching

Sometimes when creating mods you may want to target
different overhaul mods or other variations. Today mod
creators will often provide compatibility patches for
different other mods. Personally I didn't really like
this approach, as it tends to be a chore to maintain.
It is often also very error prone for end-users.

### Writing conditional patches

The main feature of this mod is the conditional patching.
It more or less boils down to be able to have the following
structure in your xml patches (e.g. `Config/XUi/xui.xml`):

```xml
<xml patcher-version="1">
  <modif condition="ul" condition="df">
    <include path="xui.ul.xml"/>
  </modif>
  <modelsif condition="SMXcore">
    <include path="xui.smx.xml"/>
  </modelsif>
  <modelse>
    <append xpath="/xui/ruleset[@name='default']">
      ...
    </append>
  </modelse>
</xml>
```

As you can see, you can also use `include` inside conditions.

#### Or'ing conditions

The `condition` attribute can be repeated and all results are
`or'ed` together for the final result.

```xml
<modif condition="ul" condition="df">...</modif>
```

#### And'ing conditions

Inside a `condition` you can define multiple comma separated
conditions that will be `and'ed` together for the final result.

```xml
<modif condition="df,smx">...</modif>
```

#### Negating a condition

Prefix a condition with an exclamation mark `!` to negate it.

```xml
<modif condition="!df" condition="!ul">...</modif>
```

### Available conditions or how to define them

As you've seen above, we have some magic conditions there like `ul`
or `df` (which btw. stand for Undead Legacy and Darkness Falls Mod).
These conditions of course do not magically appear, which is where
the underlying magic of this mod comes into play. It is all setup
in a way that it is idempotent, so every mod interested to be
compatible with this system can easily do so. Of course we can
also write our own conditions for other mods if needed :)

If there is no custom condition registered, the code will fallback
to check if any module with the name of the condition is loaded.
This should already allow for most scenarios. Custom conditions
have been added before I added this convenient fallback. Might be
enough to support anything, but let's keep the custom way for now.

## Developer information on how it is done

There are mainly two relevant parts that make this mod tick.
A globally shared class holding all conditions, which is created
dynamically (only once) and the code to patch vanilla xml patching.

### Mods wanting to create new conditions

If you only want to provide an additional condition for other mod,
you only need to include `ModXmlConditions.cs` and setup your mod
in a similar fashion as below (and implement `IsConditionTrue`):

```csharp
  private static bool IsConditionTrue()
  {
    return false;
  }
  public void InitMod(Mod mod)
  {
    Harmony harmony = new Harmony(GetType().ToString());
    harmony.PatchAll(Assembly.GetExecutingAssembly());
    Conditions = ModXmlConditions.CreateOrLoadConditionalXML();
    // Could also use Add (and check if already existing)
    Conditions["condition"] = IsConditionTrue;
  }
```

Other mods can now use this condition in their xml patches.

### Mods wanting to use conditional patching

In order to use the conditional patching, the vanilla xml
patching must be extended. You can either include this yourself
or rely on users to install this mod as a dependency. I personally
prefer the first (embedded) approach, since it makes your mod
standalone and it shouldn't add too much overhead if a lot of
mods do that, since the code is idempotent in that regard.
Meaning it doesn't matter how many mods include the same patch.

You simply need to include a copy of `ModXmlPatcher.cs`.
It contains a harmony hook that should be applied if your
mod initialization works correctly. Otherwise you can also
hook it up manually if needed like so:

```csharp
  // Hook into vanilla XML Patcher
  [HarmonyPatch(typeof(XmlPatcher))]
  [HarmonyPatch("PatchXml")]
  public class XmlPatcher_PatchXml
  {
    static bool Prefix(XmlFile _xmlFile, XmlFile _patchXml, string _patchName, ref bool __result)
    {
      // Call out to static helper function
      __result = ModXmlPatcher.PatchXml(
          _xmlFile, _patchXml, _patchName);
      // Last one wins
      return false;
    }
  }
```

Of course this means the last hook registered will win.

#### How updates for patching is future proofed

Since the last registered hook wins over all others (e.g. is
called first), we need a way to future proof this. I've chosen
an approach where you can select the version that should handle
your conditions in your xml patches. This allows you to embed a
version that is able to handle the required syntax. Any other
handler that might be called before, but is not able to handle
that version, will/should skip to parse that condition and
pass execution to the next registered harmony prefix hook.
Until it reaches your or another hook that support the version.

[![GitHub CI Compile Status][3]][2]

## Download and Install

Simply [download here from GitHub][1] and put into your A20 Mods folder:

- https://github.com/OCB7D2D/OcbCoreXmlMod/archive/master.zip (master branch)

## Changelog

### Version 0.1.0

- Initial version

## Compatibility

I've developed and tested this Mod against version a20.b218.

[1]: https://github.com/OCB7D2D/OcbCoreXmlMod/releases
[2]: https://github.com/OCB7D2D/OcbCoreXmlMod/actions/workflows/ci.yml
[3]: https://github.com/OCB7D2D/OcbCoreXmlMod/actions/workflows/ci.yml/badge.svg
