﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Common helper operations for Nodes
/// </summary>
public static class NodeHelpers
{
    /// <summary>
    ///   Properly destroys a game entity. In addition to the normal Godot Free, Destroy must be called
    /// </summary>
    public static void DestroyDetachAndQueueFree(this IEntity entity)
    {
        entity.OnDestroyed();
        entity.EntityNode.DetachAndQueueFree();
    }

    /// <summary>
    ///   Properly destroys a game entity. In addition to the normal Godot Free, Destroy must be called.
    ///   This doesn't detach the entity from the scene tree so it'll be considered valid until the end of
    ///   the current frame.
    /// </summary>
    public static void DestroyAndQueueFree(this IEntity entity)
    {
        entity.OnDestroyed();
        entity.EntityNode.QueueFree();
    }

    /// <summary>
    ///   Safely queues a Node free. Detaches from parent if attached to not leave disposed objects in scene tree.
    ///   This should always be preferred over QueueFree, except when multiple children should be deleted.
    ///   For that see <see cref="NodeHelpers.QueueFreeChildren"/>
    /// </summary>
    public static void DetachAndQueueFree(this Node node)
    {
        var parent = node.GetParent();
        parent?.RemoveChild(node);

        node.QueueFree();
    }

    /// <summary>
    ///   Call QueueFree on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">If true the children are immediately removed from the parent</param>
    public static void QueueFreeChildren(this Node node, bool detach = true)
    {
        while (true)
        {
            int count = node.GetChildCount();

            if (count < 1)
                break;

            var child = node.GetChild(count - 1);

            if (detach)
                node.RemoveChild(child);

            child.QueueFree();
        }
    }

    /// <summary>
    ///   Call Free on all Node children
    /// </summary>
    /// <param name="node">Node to delete children of</param>
    /// <param name="detach">
    ///   If true the children are also removed from the parent. Shouldn't actually have an effect.
    /// </param>
    public static void FreeChildren(this Node node, bool detach = false)
    {
        while (true)
        {
            int count = node.GetChildCount();

            if (count < 1)
                break;

            var child = node.GetChild(count - 1);

            if (detach)
                node.RemoveChild(child);

            child.Free();
        }
    }

    /// <summary>
    ///   Changes parent of this <see cref="Node"/> to a new parent. The node needs to already have parent to use this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This will likely be obsolete once https://github.com/godotengine/godot/pull/36301 is merged and
    ///     available in upcoming Godot versions.
    ///   </para>
    /// </remarks>
    public static void ReParent(this Node node, Node newParent)
    {
        if (node.GetParent() == null)
        {
            GD.PrintErr("Node needs parent to be re-parented");
            return;
        }

        node.GetParent().RemoveChild(node);
        newParent.AddChild(node);
    }

    /// <summary>
    ///   Changes parent of this <see cref="Spatial"/> to a new parent, while keeping the global position. The node
    ///   needs to already have parent to use this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This will likely be obsolete once https://github.com/godotengine/godot/pull/36301 is merged and
    ///     available in upcoming Godot versions.
    ///   </para>
    /// </remarks>
    public static void ReParentWithTransform(this Spatial node, Node newParent)
    {
        var temp = node.GlobalTransform;
        node.ReParent(newParent);
        node.GlobalTransform = temp;
    }

    /// <summary>
    ///   Get the material of this scene's model.
    /// </summary>
    /// <param name="node">Node to get material from.</param>
    /// <param name="modelPath">Path to model within the scene. If null takes scene root as model.</param>
    /// <returns>ShaderMaterial of the GeometryInstance.</returns>
    public static ShaderMaterial GetMaterial(this Node node, NodePath? modelPath = null)
    {
        GeometryInstance geometry;

        // Fetch the actual model from the scene
        if (string.IsNullOrEmpty(modelPath))
        {
            geometry = (GeometryInstance)node;
        }
        else
        {
            geometry = node.GetNode<GeometryInstance>(modelPath);
        }

        return (ShaderMaterial)geometry.MaterialOverride;
    }

    /// <summary>
    ///   Finds immediate children of node that are in a group
    /// </summary>
    /// <param name="node">The node to find children of</param>
    /// <param name="group">The group the children need to be in in</param>
    /// <typeparam name="T">The type the children are cast to before returning</typeparam>
    /// <returns>Enumerable sequence of the children</returns>
    public static IEnumerable<T> GetChildrenToProcess<T>(this Node node, string group)
    {
        foreach (Node child in node.GetChildren())
        {
            if (!child.IsInGroup(group))
                continue;

            if (child is T casted)
            {
                yield return casted;
            }
            else
            {
                GD.PrintErr("A node has been put in the ", group, " group but it isn't derived from ", typeof(T).Name);
            }
        }
    }
}
