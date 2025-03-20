namespace MTCS.Data.Helpers
{
    //public static class CategoryIDGenerator
    //{
    //    public static async Task<string> GenerateTractorCategoryId(UnitOfWork unitOfWork)
    //    {
    //        int nextId = 1;

    //        var tractorCategories = await unitOfWork.TractorRepository.GetAllTractorCategories();
    //        var tractorIds = tractorCategories.Select(c => c.TractorCateId).ToList();

    //        var numericValues = tractorIds
    //            .Where(id => int.TryParse(id, out _))
    //            .Select(id => int.Parse(id));

    //        if (numericValues.Any())
    //            nextId = numericValues.Max() + 1;

    //        string newId = $"{nextId:D3}";

    //        while (tractorIds.Contains(newId))
    //        {
    //            nextId++;
    //            newId = $"{nextId:D3}";
    //        }

    //        return newId;
    //    }

    //    public static async Task<string> GenerateTrailerCategoryId(UnitOfWork unitOfWork)
    //    {
    //        int nextId = 1;

    //        var trailerCategories = await unitOfWork.TrailerRepository.GetAllTrailerCategories();
    //        var trailerIds = trailerCategories.Select(c => c.TrailerCateId).ToList();

    //        var numericValues = trailerIds
    //            .Where(id => int.TryParse(id, out _))
    //            .Select(id => int.Parse(id));

    //        if (numericValues.Any())
    //            nextId = numericValues.Max() + 1;

    //        string newId = $"{nextId:D3}";

    //        while (trailerIds.Contains(newId))
    //        {
    //            nextId++;
    //            newId = $"{nextId:D3}";
    //        }

    //        return newId;
    //    }
    //}
}
