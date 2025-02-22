﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Represents an early multicellular species that is composed of multiple cells
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
public class EarlyMulticellularSpecies : Species
{
    public EarlyMulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
    }

    [JsonProperty]
    public CellLayout<CellTemplate> Cells { get; private set; } = new();

    [JsonProperty]
    public List<CellType> CellTypes { get; private set; } = new();

    /// <summary>
    ///   All organelles in all of the species' placed cells (there can be a lot of duplicates in this list)
    /// </summary>
    [JsonIgnore]
    public IEnumerable<OrganelleTemplate> Organelles => Cells.SelectMany(c => c.Organelles);

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    public override void OnEdited()
    {
        RepositionToOrigin();
        UpdateInitialCompounds();

        // Make certain these are all up to date
        foreach (var cellType in CellTypes)
        {
            cellType.CalculateRotationSpeed();
        }
    }

    public override void RepositionToOrigin()
    {
        // TODO: should this actually reposition things as the cell at index 0 is always the colony leader so if it
        // isn't centered, that'll cause issues?
        // var centerOfMass = Cells.CenterOfMass;

        var centerOfMass = Cells[0].Position;

        foreach (var cell in Cells)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            cell.Position -= centerOfMass;
        }
    }

    public override void UpdateInitialCompounds()
    {
        var simulation = SimulationParameters.Instance;

        var rusticyanin = simulation.GetOrganelleType("rusticyanin");
        var chemo = simulation.GetOrganelleType("chemoplast");
        var chemoProtein = simulation.GetOrganelleType("chemoSynthesizingProteins");

        if (Organelles.Any(o => o.Definition == rusticyanin))
        {
            SetInitialCompoundsForIron();
        }
        else if (Organelles.Any(o => o.Definition == chemo ||
                     o.Definition == chemoProtein))
        {
            SetInitialCompoundsForChemo();
        }
        else
        {
            SetInitialCompoundsForDefault();
        }
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (EarlyMulticellularSpecies)mutation;

        Cells.Clear();

        foreach (var cellTemplate in casted.Cells)
        {
            Cells.Add((CellTemplate)cellTemplate.Clone());
        }

        CellTypes.Clear();

        foreach (var cellType in casted.CellTypes)
        {
            CellTypes.Add((CellType)cellType.Clone());
        }
    }

    public override object Clone()
    {
        var result = new EarlyMulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        foreach (var cellTemplate in Cells)
        {
            result.Cells.Add((CellTemplate)cellTemplate.Clone());
        }

        foreach (var cellType in CellTypes)
        {
            result.CellTypes.Add((CellType)cellType.Clone());
        }

        return result;
    }

    private void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();

        // TODO: modify these numbers based on the cell count or something more accurate
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("atp"), 60);
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("glucose"), 30);
    }

    private void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("iron"), 30);
    }

    private void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("hydrogensulfide"), 30);
    }
}
