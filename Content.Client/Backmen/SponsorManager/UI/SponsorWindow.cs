﻿using System.Linq;
using Content.Client.Backmen.GhostTheme;
using Content.Client.Backmen.Sponsors;
using Content.Corvax.Interfaces.Client;
using Content.Corvax.Interfaces.Shared;
using Content.Shared.Backmen.GhostTheme;
using Content.Shared.Ghost;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Backmen.SponsorManager.UI;

[GenerateTypedNameReferences]
public sealed partial class SponsorWindow : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ISharedSponsorsManager _clientSponsorsManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public SponsorWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        GhostTheme.OnItemSelected += GhostThemeChange;
    }

    private EntityUid? _ghostDummy;

    private List<GhostThemePrototype>? _ghostThemePrototypes;

    [ValidatePrototypeId<EntityPrototype>]
    private const string ObserverPrototypeName = "MobObserver";

    protected override void Opened()
    {
        base.Opened();

        if (_ghostThemePrototypes == null)
        {
            _ghostThemePrototypes = new();
            foreach (var proto in _clientSponsorsManager.GetClientPrototypes())
            {
                if (!_prototypeManager.TryIndex<GhostThemePrototype>(proto, out var ghostProto))
                {
                    continue;
                }
                _ghostThemePrototypes.Add(ghostProto);
            }
        }

        var selectedGhost = _cfg.GetCVar(Shared.Backmen.CCVar.CCVars.SponsorsSelectedGhost);
        var selectedInt = -1;

        GhostTheme.AddItem(Loc.GetString("sponsor-win-manager-ghost-default"), id: -1);
        foreach (var (themePrototype,id) in _ghostThemePrototypes.Select((x,i)=>(x,i)))
        {
            GhostTheme.AddItem(Loc.GetString(themePrototype.Name), id);
            if (selectedGhost == themePrototype.ID)
            {
                selectedInt = id;
            }
        }

        //setVars
        Tier.Text = Loc.GetString($"sponsor-win-manager-tier-{(_clientSponsorsManager as SponsorsManager)?.Tier}");
        GhostTheme.SelectId(selectedInt);
        GhostPreview(selectedInt);
    }

    private float _accumulatedTime;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _accumulatedTime += args.DeltaSeconds;
        GhostView.OverrideDirection = (Direction) ((int) _accumulatedTime % (4 * 3) / 3 * 2);
        if (_accumulatedTime > 200_000)
        {
            _accumulatedTime = 0;
        }
    }

    private void GhostPreview(int id)
    {
        if (_ghostDummy.HasValue)
        {
            _entityManager.DeleteEntity(_ghostDummy);
            _ghostDummy = null;
        }
        _ghostDummy = _entityManager.SpawnEntity(ObserverPrototypeName, MapCoordinates.Nullspace);
        if (id >= 0 && _ghostThemePrototypes != null && _ghostThemePrototypes.Count > id)
        {
            _entityManager.SystemOrNull<GhostThemeSystem>()?.Apply(_ghostDummy.Value, _ghostThemePrototypes[id]);
        }

        _entityManager.EnsureComponent<SpriteComponent>(_ghostDummy.Value).Visible = true;
        GhostView.SetEntity(_ghostDummy);
    }

    public override void Close()
    {
        base.Close();
        if (_ghostDummy.HasValue)
        {
            _entityManager.DeleteEntity(_ghostDummy);
            _ghostDummy = null;
        }

        Tier.Text = "";
        GhostView.SetEntity(null);
        GhostTheme.Clear();
    }

    private void GhostThemeChange(OptionButton.ItemSelectedEventArgs e)
    {
        GhostPreview(e.Id);
        GhostTheme.SelectId(e.Id);
        var selectedGhost = "";

        foreach (var (themePrototype,id) in _ghostThemePrototypes!.Select((x,i)=>(x,i)))
        {
            if (e.Id == id)
            {
                selectedGhost = themePrototype.ID;
            }
        }
        _cfg.SetCVar(Shared.Backmen.CCVar.CCVars.SponsorsSelectedGhost, selectedGhost);
        _cfg.SaveToFile();
    }
}

