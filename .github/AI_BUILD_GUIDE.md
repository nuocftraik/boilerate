# HƯỚNG DẪN DÙNG AI ĐỂ BUILD DỰ ÁN TỪ BOILERPLATE DOCS

Tài liệu này dùng cho kỹ sư trưởng để điều khiển các AI Agent (GitHub Copilot Copilot, Cursor, Cline, Claude, v.v.) tự động build dự án dựa trên bộ thư mục `docs/`.

## 4 LƯU Ý SỐNG CÒN KHI ĐIỀU KHIỂN AI

1. **Sử dụng lệnh từng phần (Step-by-step):** 
   Đừng bao giờ bắt AI làm toàn bộ 38 bước cùng lúc do giới hạn Context/Token. Bạn nên điều khiển nó bằng cách gõ:
   - "Start Phase 1, Step 1"
   - (AI làm xong) -> Gõ "Next" (Nó sẽ tự chuyển sang Step 2)
   - (AI làm xong) -> Gõ "Next"...

2. **Duy trì file `.github/copilot-instructions.md` (hoặc `.cursorrules`):** 
   Bộ tệp này đã được cấu hình chặt chẽ Quy tắc đặt tên, Luồng Dependency, Layering. Bất kỳ AI xịn nào cũng sẽ bí mật nhúng tệp này vào prompt. Nó giúp AI không bao giờ quên nguyên tắc tử huyệt: Layer Domain KHÔNG BAO GIỜ được reference Infrastructure!

3. **Cung cấp tên Solution `{ProjectName}` ngay từ đầu:** 
   Trong bộ thư mục `docs/` chứa rất nhiều biến môi trường `{ProjectName}`. Hãy cho AI biết luôn tên dự án trong prompt phụ.
   - Ví dụ: "By the way, replace `{ProjectName}` with `EcoSystem` for this execution."

4. **Kiểm tra Commit (Critical):** 
   Sau mỗi Phase hoặc sau 1 Step cốt lõi, bạn phải tự tay Commit Git lại một lần (`git add . && git commit -m "Done Phase X"`). Nếu AI xuất code lỗi hay code "ảo" ở thư mục sau, bạn dễ dàng `git reset --hard` để cho nó làm lại bước đó thay vì đập đi xây lại toàn bộ system.

---

## MASTER PROMPT (Copy phần bên dưới paste vào AI Chat khi bắt đầu dự án mới)

```text
# ROLE
You are an Expert .NET latest version / Clean Architecture Developer and an Autonomous coding agent.

# CONTEXT
I have provided a documentation folder at `docs/` containing the complete blueprint to build a Clean Architecture solution from scratch. The master roadmap is located in `docs/BUILD_INDEX.md`.

# PROJECT VARIABLE BINDING
For all operations in this session, replace `{ProjectName}` mapping variable with my target project name: [ĐIỀN TÊN DỰ ÁN CỦA BẠN VÀO ĐÂY, VD: MyEcoStore]

# OBJECTIVE
Your goal is to build this complete boilerplate source code exactly as instructed in the blueprint incrementally.

# EXECUTION RULES
Whenever I ask you to process a Phase or a Step, you MUST strictly follow this loop:
1. Locate & Read: Open and read `docs/BUILD_INDEX.md` first. Then, read the specific `docs/BUILD_XX_...md` file for the exact step.
2. Plan: Formulate a short plan of which files you will create/modify.
3. Execute: 
   - Execute terminal commands correctly (dotnet new, dotnet sln, etc.).
   - Generate C# code strictly adopting the architecture dependencies (Shared -> Domain -> Application -> Infrastructure -> Host).
   - NEVER hallucinate packages or skip settings (`appsettings.json`, `Program.cs`).
4. Verify: Run `dotnet build` to ensure compilation succeeds.
5. Report & Pause: Summarize what was completed and ask for my permission to proceed. DO NOT proceed without me explicitly saying "Next" or "Continue".

# TRIGGERS & COMMANDS
- "Start Phase X": Identify all steps in Phase X from BUILD_INDEX.md and execute the first step.
- "Next": Move to the subsequent step in the roadmap.
- "Fix errors": Diagnose compiler errors and fix them following `.cursorrules`.

# INITIALIZATION 
Acknowledge these instructions. Read `docs/BUILD_INDEX.md` right now, give me a brief summary of PHASE 1, and ask if I'm ready to "Start Phase 1, Step 1".```