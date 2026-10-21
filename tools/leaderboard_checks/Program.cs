using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VanDao.Data;
using VanDao.Game;
var count=0;
void Check(bool value,string name){if(!value)throw new Exception(name);count++;}
Player.AdminEnabled=false;
var p=new Player();
Check(GameService.LeaderboardProgress(p)==0,"new player has no completed stages");
p.AreaMax=2;Check(GameService.LeaderboardProgress(p)==1,"unlocked stage is not counted as completed");
p.AreaMax=GameData.Areas;Check(GameService.LeaderboardProgress(p)==GameData.Areas-1,"boss not credited until killed");
p.Cleared="0.0";Check(GameService.LeaderboardProgress(p)==GameData.Areas,"boss kill credited");
p.ChapterMax=1;p.AreaMax=1;Check(GameService.LeaderboardProgress(p)==GameData.Areas,"new chapter unlocked does not claim first stage");
p.AreaMax=2;Check(GameService.LeaderboardProgress(p)==101,"chapter progress outranks prior chapter");
p.DiffProg="1.0=2";Check(GameService.LeaderboardProgress(p)==10001,"hard stage outranks normal chapters");
p.Cleared="2.0";Check(GameService.LeaderboardProgress(p)==20000+GameData.Areas,"nightmare completed boss");
Check(GameService.LeaderboardPetPower(p)==0,"no pets gives zero");
var pet=new Pet{DefId="hoa_linh_ho",Level=1};p.Pets.Add(pet);
var first=GameService.LeaderboardPetPower(p);Check(first>0,"known pet contributes power");
p.Pets.Add(new Pet{DefId="unknown",Level=999});Check(GameService.LeaderboardPetPower(p)==first,"unknown pet ignored");
p.Pets.Add(new Pet{DefId="hoa_linh_ho",Level=10});Check(GameService.LeaderboardPetPower(p)>first,"strongest pet selected");
var strongest=GameService.LeaderboardPetPower(p);p.Pets.Add(pet);Check(GameService.LeaderboardPetPower(p)==strongest,"collection size does not inflate strongest pet score");
var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Logging.ClearProviders();
await using var server=builder.Build();var oldSchema=false;
server.MapGet("/rest/v1/leaderboard_public",(Microsoft.AspNetCore.Http.HttpRequest request)=> {
    if(oldSchema && request.Query["select"].ToString().Contains("progress"))
        return Microsoft.AspNetCore.Http.Results.BadRequest(new {message="column progress does not exist"});
    return Microsoft.AspNetCore.Http.Results.Json(new[]{new {player_id=p.Id,name="Tester",realm=0,stage=0,realm_name="Luyện Khí",stage_name="Tầng 1",power=500,realm_rank=8,power_rank=7,flagged=false,progress=101,pet_power=123,progress_rank=2,pet_rank=3}});
});
await server.StartAsync();
var url=server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
var options=new LeaderboardOptions{Url=url,AnonKey="test"};
var logger=Microsoft.Extensions.Logging.Abstractions.NullLogger<LeaderboardService>.Instance;
var service=new LeaderboardService(options,logger);
p.Tainted=false;
foreach(var entry in new[]{("realm",8L),("power",7L),("progress",2L),("pet",3L)}) {
 var result=await service.GetAsync(p,entry.Item1);
 Check(result.Error==null&&result.Rows.Single().Rank==entry.Item2,"correct rank for "+entry.Item1);
 Check(result.Rows.Single().Progress==101&&result.Rows.Single().PetPower==123,"extended score payload for "+entry.Item1);
}
oldSchema=true;service=new LeaderboardService(options,logger);
Check((await service.GetAsync(p,"realm")).Error==null,"old schema realm fallback works");
Check((await service.GetAsync(p,"progress")).Error?.Contains("supabase.sql")==true,"old schema progress migration message");
Check((await service.GetAsync(p,"pet")).Error?.Contains("supabase.sql")==true,"old schema pet migration message");
await server.StopAsync();
Console.WriteLine($"PASS: {count} leaderboard progress and pet scoring checks.");
