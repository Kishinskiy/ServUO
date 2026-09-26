================================================================================

  **UO WILDLANDS - TOWN HOUSE SYSTEM**

  **Setup Guide & Feature Reference**

================================================================================



FILES
-----
  TownHouseController.cs  — Core system (controller, region, utilities, GM tools)
  TownHouseSignGump.cs    — Player-facing sign gump (purchase, access lists, etc.)
  TownHouseSystem.cs      — Server registry and GM index gump

All three files go in the same folder, e.g.:
  Scripts/Custom Commands/Townhouses/

The following additional files must also be patched to support addon deed
placement inside TownHouse regions:

  BaseAddonDeed.cs   — Allows most standard crafted addon deeds (soul forges,
                       looms, etc.) to be placed inside TownHouse regions.
  Mannequin.cs       — Allows Mannequin deeds to be placed inside TownHouse regions.
  Steward.cs         — Allows Steward deeds to be placed inside TownHouse regions.
  DaviesLocker.cs    — Allows Davies Locker deeds to be placed and used inside
                       TownHouse regions.
  
Without these patches, players will receive "You can only place this in a house
that you own" when attempting to use deeds inside a TownHouse.


--------------------------------------------------------------------------------
**GM SETUP — HOW TO CREATE A TOWN HOUSE**
--------------------------------------------------------------------------------

1. **PLACE THE SIGN**
   Use [add TownHouseController to place a town house sign at the desired
   location. The sign acts as the control point for the entire property.

   The sign must be "set movable false" either as a GM command or via [props
   to remain on server restart. It can be relocated with the [move command.

2. **OPEN THE SIGN GUMP**
   Double-click the sign (or use [TownHouses to find it in the index).
   The GM Configuration panel appears on the right side of the gump.

3. **SET THE BOUNDS**
   Click "Set Bounds" — you will be prompted to target two corners of the
   property. Target the floor at opposite corners (e.g. front-left and
   back-right). The system will calculate the rectangle automatically.

4. **HIGHLIGHT BOUNDS (OPTIONAL)**
   Click "Highlight Bounds" to place temporary markers showing the exact
   region. Markers auto-delete after 30 seconds. Useful for verifying the
   bounds before opening the property for sale.

5. **CONFIGURE SETTINGS**
   In the GM Configuration panel you can set:
     - Price:    Purchase price in gold (drawn from bank)
     - Locks:    Maximum number of lockdowns allowed (default 1500)
     - Z-Range:  Vertical range of the region (min Z to max Z)
   Click "APPLY SETTINGS" to save.

6. **SET STATE**
   Click "Cycle State" to toggle between Purchasable and Transferable.
   Set it to Purchasable so players can buy it from the sign.

7. **WIPE POLICY (OPTIONAL)**
   Click "Wipe Policy" to toggle between:
     - LeaveAsIs:          Items remain when the house changes hands (default)
     - WipeItemsInBounds:  All items inside are deleted on eviction/abandonment

8. **DONE**
   The sign will display "For Sale: X gp" when single-clicked.
   Players can now double-click the sign to purchase the property.


--------------------------------------------------------------------------------
**GM MANAGEMENT — [TownHouses COMMAND**
--------------------------------------------------------------------------------

Type [TownHouses to open the Town House Global Index Gump.
This shows all registered town houses with the following columns:

  MAP      — Which facet the house is on
  LOCATION — X, Y, Z coordinates of the sign
  STATE    — Purchasable / Transferable
  OWNER    — Current owner name, or "- Unowned -"

Actions available per row:
  Go     — Teleports you to the sign location
  Open   — Opens the sign gump for that house (same as double-clicking the sign)
  Evict  — Immediately removes the owner, resets the house to Purchasable,
            and clears all access lists. Items remain in place but are
            immovable until the new owner re-secures or re-lockdowns them.

Click Refresh to update the list.


--------------------------------------------------------------------------------
**PLAYER FEATURES**
--------------------------------------------------------------------------------

**PURCHASING**
  Players double-click the sign when State = Purchasable.
  Gold is withdrawn from their bank account.
  Ownership is assigned immediately.

**SIGN GUMP — MAIN TAB**
  Shows property status, owner name, lockdown count, secure count,
  total area in tiles, and available actions based on access level.

**PUBLIC / PRIVATE TOGGLE** (Owner only)
  Houses are PRIVATE by default. Non-friends cannot enter the region at all —
  they are blocked at the boundary regardless of whether anything is secured.
  The owner can toggle Public/Private from the sign gump. When set to Public,
  all players may enter freely but secured items are still protected.
  Bans are enforced in both modes.

**ITEM SECURITY** (Friends and above)
  Lockdown Item  — Prevents an item from being moved (up to the lockdown limit).
  Secure Item    — Locks any item (including containers and custom storage items
                   such as VirtualStorageChest); only Friends/CoOwners/Owner can
                   open or interact with it. The item displays "Secured" in its
                   tooltip.
  Release Item   — Returns an item to movable/accessible state.
                   Requires Co-Owner access or above.

**ADDON DEED PLACEMENT** (Friends and above)
  Most crafted addon deeds (soul forges, looms, forges, etc.) can be placed
  inside a TownHouse region provided the patched BaseAddonDeed.cs is installed.
  The following items require their own individually patched files:
    - Mannequin Deed
    - Steward Deed
    - Davies' Locker Deed

  Wall-mount requirements, collision detection, and door proximity checks all
  still apply normally inside TownHouses.

**ACCESS LISTS** (Co-Owners and above)
  Friends   — Can enter when the house is private, lockdown items, and open
               secured items.
  Co-Owners — All friend rights plus can manage friends, bans, and release
               lockdowns/secures. Only the Owner can add or remove Co-Owners.
  Bans      — Banned players cannot enter the house region regardless of
               Public/Private setting.

  All lists support pagination. Click the button next to each list name
  to open the management screen. Click the red X button next to a name
  to remove them.

  When a player is added to any list they are automatically removed from
  all other lists — the last action always wins. For example, adding a
  banned player as a Friend removes them from the ban list.

**PLAYER SALE** (Owner only)
  The owner can set a sale price via "Sell House (Set Price)".
  Once set, any player can purchase it directly from the sign.
  Gold goes to the seller's bank. Cancel Sale removes the listing.

**TRANSFER** (Owner only — Transferable state only)
  Targets another player and transfers full ownership to them.
  All access lists are cleared on transfer.

**ABANDON** (Owner only)
  Immediately clears ownership and resets to Purchasable.
  Items remain in place but immovable for the next owner to claim.


--------------------------------------------------------------------------------
**REGION BEHAVIOUR**
--------------------------------------------------------------------------------

  - Private by default — non-friends are blocked from entering the region.
  - Public toggle — owner can open the house to all players while still
    protecting secured items and enforcing bans.
  - Ban enforcement — banned players cannot step into the house region
    regardless of Public/Private setting.
  - Secured item access — non-friends attempting to interact with a secured
    item receive an access denied message.


--------------------------------------------------------------------------------
**ITEM BEHAVIOUR ON EVICTION / ABANDONMENT**
--------------------------------------------------------------------------------

  When a house is evicted (GM) or abandoned (player):
  - All lockdown and secure registrations are cleared.
  - Items remain physically in place but immovable.
  - IsLockedDown and IsSecure flags are cleared so the engine does not
    treat them as belonging to the old owner.
  - The new owner can re-lockdown or re-secure items after purchasing.
  - If WipePolicy is set to WipeItemsInBounds, all items inside the
    bounds are deleted instead.


--------------------------------------------------------------------------------
**NOTES**
--------------------------------------------------------------------------------

  - [add TownHouseController is the only command needed to place a house.
  - Each sign is self-contained — no external region files or XML needed.
  - Multiple town houses can exist on the same shard with no conflicts.
  - TownHouseConfig.UseAccountOwnership (in TownHouseSystem.cs) controls
    whether ownership is checked per-character (false) or per-account (true).
    Default is false.

================================================================================
