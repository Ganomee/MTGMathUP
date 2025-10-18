# Urgent Fixes Needed

## Critical Issues Found

### 1. ❌ Component Library Page Blank (JavaScript Error)
**Error**: `ReferenceError: R is not defined`
**Location**: `/components` page
**Cause**: Build issue with Svelte component compilation

**Fix**: The page needs to be rebuilt. The issue is that the scryfall-api import is not working in the browser context.

**Action Items**:
- [ ] Mock the scryfall-api for the component library page (dev/demo mode only)
- [ ] Add error boundaries to catch component failures
- [ ] Test in dev mode first before building

### 2. ❌ Mana Font Invisible
**Issue**: HTML shows `<i class="ms ms-w ms-cost">` but no symbols visible
**Cause**: Font files not loading

**Fix Needed**:
```css
/* app.css needs to import mana-font with proper path */
@import 'mana-font/css/mana.min.css';
```

Also need to verify font files are copied to build output.

**Action Items**:
- [ ] Change import to `mana.min.css`
- [ ] Add font-face declarations if needed
- [ ] Check vite.config.ts for asset handling
- [ ] Verify fonts in build output

### 3. ❌ Column Name Mismatch
**Error**: `column "manacost" of relation "cards" does not exist`
**Cause**: Backend uses `ManaCost` (PascalCase) but database uses `mana_cost` (snake_case)

**Backend Entity** (`Card.cs`):
```csharp
public string ManaCost { get; set; }
```

**Database Schema** (migrations):
```sql
mana_cost TEXT
```

**Frontend PGlite**:
```sql
mana_cost TEXT
```

**Fix**: Backend needs to map property names properly OR change database to use ManaCost

**Action Items**:
- [ ] Check EF Core migrations for column names
- [ ] Update backend entity with Column attribute if needed:
  ```csharp
  [Column("mana_cost")]
  public string ManaCost { get; set; }
  ```
- [ ] OR update all frontend references to use mana_cost consistently

### 4. ❌ UUID Format Errors
**Error**: `invalid input syntax for type uuid: "20"`
**Cause**: Backend is returning integer IDs as strings, but PGlite expects UUID format

**Fix Needed**:
- Backend API is returning numeric IDs (e.g., "20") not UUIDs
- PGlite schema expects UUIDs
- Need to either:
  1. Change PGlite schema to use INTEGER ids
  2. Change backend to use UUIDs
  3. Add ID conversion in sync layer

**Action Items**:
- [ ] Check backend Card entity - does it use int or Guid for Id?
- [ ] Update PGlite schema to match backend (likely INTEGER not UUID)
- [ ] Update sync-manager to handle integer IDs

### 5. ❌ Empty Tables in PGlite Mode
**Cause**: Sync is failing due to above UUID and column name errors
**Result**: No data gets synced to local PGlite

**Fix**: Resolve issues 3 & 4 above

### 6. ⚠️ Scryfall Import Issues
**Problems**:
- No visible status updates
- Refresh button doesn't work
- Missing "Get New Images" button
- Missing "Force Full Download" checkbox
- Last import date not shown

**Action Items**:
- [ ] Add proper status display to import page
- [ ] Implement refresh button functionality
- [ ] Add "Get New Images" button with force checkbox
- [ ] Display last import timestamp from backend
- [ ] Add loading states and progress indicators

### 7. ⚠️ Backend Connection Toggle Unclear
**Current**: Shows online/offline
**Needed**: 
- "Online - Connected to backend server"
- "Offline - Using only PGlite"

**Action Items**:
- [ ] Update toggle text to be more descriptive
- [ ] Add tooltips explaining what each mode does
- [ ] Show sync status more clearly

## Priority Order

1. **CRITICAL**: Fix column name mismatch (manaCost vs mana_cost)
2. **CRITICAL**: Fix UUID vs INTEGER ID mismatch  
3. **HIGH**: Fix mana font loading
4. **HIGH**: Fix component library page JavaScript error
5. **MEDIUM**: Fix Scryfall import UI
6. **LOW**: Improve connection toggle text

## Quick Diagnostic Commands

```bash
# Check backend database schema
psql -U postgres -d mtgmullagain -c "\d cards"

# Check what backend API returns
curl http://localhost:5000/api/cards | jq '.[0]'

# Check PGlite in browser console
db.exec("SELECT * FROM cards LIMIT 1")

# Check mana font files
ls -la frontend/node_modules/mana-font/fonts/
```

## Recommended Fix Strategy

### Phase 1: Database Schema Alignment
1. Identify if backend uses INTEGER or UUID for IDs
2. Update PGlite schema to match
3. Fix column name casing (mana_cost vs ManaCost)

### Phase 2: Frontend Fixes
1. Fix mana font CSS import
2. Fix component library page  
3. Update sync-manager error handling

### Phase 3: UI Improvements
1. Scryfall import improvements
2. Better status messages
3. Improved error displays

## Files That Need Changes

### Backend
- `backend/src/MtgMullagain.Core/Entities/Card.cs` - Check ID type and column mapping
- Backend migrations - Check actual column names

### Frontend
- `frontend/src/lib/db/pglite-client.ts` - Fix schema (UUID vs INTEGER, column names)
- `frontend/src/lib/db/sync-manager.ts` - Fix ID handling
- `frontend/src/app.css` - Fix mana font import
- `frontend/src/routes/components/+page.svelte` - Fix scryfall-api usage
- `frontend/src/routes/import/+page.svelte` - Add missing UI elements


