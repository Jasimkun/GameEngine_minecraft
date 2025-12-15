using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "조합법 생성")]
public class CraftingRecipe : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemType type;
        public int count;
    }   //재료

    [Serializable]
    public struct Product
    {
        public ItemType type;
        public int count;
    }   //결과물

    public string displayName;
    public List<Ingredient> inputs = new();
    public List<Product> outputs = new();
}
