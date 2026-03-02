using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_Store.Migrations
{
    public partial class AddOrderStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserID",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "dbo",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                schema: "dbo",
                newName: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderDetails",
                newName: "Price");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders",
                column: "UserID",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Orders",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                newName: "OrderDetails",
                newSchema: "dbo");

            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "dbo",
                table: "OrderDetails",
                newName: "UnitPrice");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                schema: "dbo",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserID",
                schema: "dbo",
                table: "Orders",
                column: "UserID",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
