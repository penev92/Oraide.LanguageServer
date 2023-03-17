
//  - List of actor definitions, each with its location, to be able to jump to a definition. (only from Inherits:)
//  - All files as arrays of parsed entities (actor/trait/property) to make lookups by line number fast af. (to easily find what the IDE/client is referencing on every request)
//
//  - Parse everything as YamlNodes?
//  Level1 nodes (top-level nodes) are either actor or weapon definitions. They can only reference other top-level nodes.
//  Level2 nodes (one-tab indentation) are traits for actors, but properties for weapons...
//	For actors they reference TraitInfos. Their values can reference top-level nodes.
//	For weapons they reference WeaponInfo properties. Their values can reference top-level nodes, an IProjectileInfo implementation or an IWarhead implementation.
//  Level3 nodes (two-tab indentation) are trait properties for actors,
//	For actors they are trait properties. Their values can reference top-level nodes, condition string literals or a name string literal defined by another Level3 node.
//	For weapons they are either ProjectileInfo or Warhead properties.
//
//
//  Use cases:
//	User opens OpenRA folder.	( ./OpenRA/ )
//	User opens mods folder.	( ./OpenRA/mods/ )
//	User opens a single mod's folder.	( ./OpenRA/mods/d2k/ )
//	User opens a subfolder of any mod.	( ./OpenRA/mods/d2k/rules/ )
