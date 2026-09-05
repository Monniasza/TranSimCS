# RULES

* Merging is allowed only into lanes that either expand into the merge source, or do not expand into it.
* If a merge amount from side `A` is a merge, then lane merges into side opposite of `A`
* Double merges are not allowed. A workaround is a double-sided merge (from both left and right into the middle lane) or two one-sided merges.

## Validity constraints

### For each `LaneMappingInput input` with index of `int index` and total lane count of `int count`
```
-- prohibit merging into the right edge
input.LeftAmount.IsMerge & !input.RightAmount.IsMerge -> (count - index) > 1
-- prohibit merging into the left edge
input.RightAmount.IsMerge & !input.LeftAmount.IsMerge -> index > 0
-- prohibit both left expand and right merge
input.RightAmount.IsMerge !& input.LeftAmount.IsExpand
-- prohibit both right expand and left merge
input.LeftAmount.IsMerge !& input.RightAmount.IsExpand

```

### For each adjacent pair of `LaneMappingInput a, b`
```
-- prohibit double merges into right
a.LeftAmount.IsMerge !& b.LeftAmount.IsMerge
-- prohibit merges into each other
a.LeftAmount.IsMerge !& b.RightAmount.IsMerge
-- prohibit double merges into left
a.RightAmount.IsMerge !& b.RightAmount.IsMerge
```