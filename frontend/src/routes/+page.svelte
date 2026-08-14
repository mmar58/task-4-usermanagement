<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { api } from "$lib/api";
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import { formatDistanceToNow } from "date-fns";
    import { Lock, Unlock, Trash2, Eraser, LogOut } from "@lucide/svelte";

    interface User {
        id: string;
        name: string;
        email: string;
        status: number; // 0 Active, 1 Blocked, 2 Unverified
        lastSeen: string | null;
    }

    let users = $state<User[]>([]);
    let selectedIds = $state<Set<string>>(new Set());
    let isLoading = $state(true);
    let isCheckingAuth = $state(true);
    let filterText = $state("");

    onMount(() => {
        const token = localStorage.getItem("token");
        if (!token) {
            goto("/login");
            return;
        }
        isCheckingAuth = false;
        fetchUsers();
    });

    async function fetchUsers() {
        try {
            isLoading = true;
            const res = await api.get("/users");
            users = res.data;
            // Clear selection on refresh
            selectedIds = new Set();
        } catch (e) {
            // Error handling is managed by api interceptor
        } finally {
            isLoading = false;
        }
    }

    const filteredUsers = $derived(
        users.filter(
            (u) =>
                u.name.toLowerCase().includes(filterText.toLowerCase()) ||
                u.email.toLowerCase().includes(filterText.toLowerCase()),
        ),
    );

    const allSelected = $derived(
        filteredUsers.length > 0 && selectedIds.size === filteredUsers.length,
    );
    // Note: shadcn-svelte Checkbox doesn't directly take 'indeterminate' as a boolean prop like native input
    // We'll manage it visually via checked state or just skip indeterminate for simplicity and use checked for 'all selected'
    // Let's rely on checked={allSelected} for the header.

    function toggleSelectAll() {
        if (allSelected) {
            selectedIds = new Set();
        } else {
            selectedIds = new Set(filteredUsers.map((u) => u.id));
        }
    }

    function toggleSelect(id: string) {
        const newSet = new Set(selectedIds);
        if (newSet.has(id)) {
            newSet.delete(id);
        } else {
            newSet.add(id);
        }
        selectedIds = newSet;
    }

    async function handleAction(action: "block" | "unblock" | "delete") {
        if (selectedIds.size === 0) return;
        const ids = Array.from(selectedIds);

        try {
            if (action === "delete") {
                await api.delete("/users", { data: ids });
            } else {
                await api.put(`/users/${action}`, ids);
            }
            await fetchUsers();
        } catch (e) {
            // Handled by interceptor
        }
    }

    function formatTime(dateString: string | null) {
        if (!dateString) return "Never";
        return formatDistanceToNow(new Date(dateString), { addSuffix: true });
    }

    function getStatusText(status: number) {
        switch (status) {
            case 0:
                return "Active";
            case 1:
                return "Blocked";
            case 2:
                return "Unverified";
            default:
                return "Unknown";
        }
    }

    function handleLogout() {
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        goto("/login");
    }
</script>

{#if !isCheckingAuth}
    <div class="min-h-screen bg-white p-8">
        <div class="max-w-6xl mx-auto space-y-4">
            <!-- Toolbar -->
            <div class="flex items-center justify-between">
                <div class="flex items-center space-x-2">
                    <Button
                        variant="outline"
                        size="sm"
                        class="h-9 text-blue-600 border-blue-200 bg-blue-50/50 hover:bg-blue-100/50 flex items-center space-x-2"
                        onclick={() => handleAction("block")}
                    >
                        <Lock class="w-4 h-4" />
                        <span>Block</span>
                    </Button>
                    <Button
                        variant="outline"
                        size="icon"
                        class="h-9 w-9 text-blue-600 border-blue-200 bg-blue-50/50 hover:bg-blue-100/50"
                        onclick={() => handleAction("unblock")}
                        title="Unblock"
                    >
                        <Unlock class="w-4 h-4" />
                    </Button>
                    <Button
                        variant="outline"
                        size="icon"
                        class="h-9 w-9 text-red-600 border-red-200 bg-red-50/50 hover:bg-red-100/50"
                        onclick={() => handleAction("delete")}
                        title="Delete"
                    >
                        <Trash2 class="w-4 h-4" />
                    </Button>
                    <Button
                        variant="outline"
                        size="icon"
                        class="h-9 w-9 text-red-600 border-red-200 bg-red-50/50 hover:bg-red-100/50"
                        onclick={() => {
                            selectedIds = new Set();
                        }}
                        title="Clear Selection"
                    >
                        <Eraser class="w-4 h-4" />
                    </Button>
                </div>

                <div class="flex items-center space-x-4">
                    <div class="relative w-64">
                        <Input
                            type="text"
                            placeholder="Filter"
                            bind:value={filterText}
                            class="h-9 border-gray-200"
                        />
                    </div>
                    <Button
                        variant="outline"
                        size="sm"
                        class="h-9 text-gray-600 hover:bg-gray-100"
                        onclick={handleLogout}
                        title="Logout"
                    >
                        <LogOut class="w-4 h-4 mr-2" />
                        Logout
                    </Button>
                </div>
            </div>

            <!-- Table -->
            <div class="border rounded-lg overflow-hidden">
                <table class="w-full text-sm text-left">
                    <thead class="bg-gray-50/50 text-gray-900 border-b">
                        <tr>
                            <th class="w-12 px-4 py-3 text-center">
                                <Checkbox
                                    checked={allSelected}
                                    onCheckedChange={toggleSelectAll}
                                    class="border-gray-300 data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
                                />
                            </th>
                            <th class="px-4 py-3 font-semibold">Name</th>
                            <th class="px-4 py-3 font-semibold">Email</th>
                            <th class="px-4 py-3 font-semibold">Status</th>
                            <th class="px-4 py-3 font-semibold">Last seen</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y">
                        {#if isLoading}
                            <tr>
                                <td
                                    colspan="5"
                                    class="px-4 py-8 text-center text-gray-500"
                                    >Loading users...</td
                                >
                            </tr>
                        {:else if filteredUsers.length === 0}
                            <tr>
                                <td
                                    colspan="5"
                                    class="px-4 py-8 text-center text-gray-500"
                                    >No users found.</td
                                >
                            </tr>
                        {:else}
                            {#each filteredUsers as user}
                                <tr
                                    class="hover:bg-gray-50/50 transition-colors"
                                >
                                    <td class="px-4 py-3 text-center">
                                        <Checkbox
                                            checked={selectedIds.has(user.id)}
                                            onCheckedChange={() =>
                                                toggleSelect(user.id)}
                                            class="border-gray-300 data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
                                        />
                                    </td>
                                    <td class="px-4 py-3">
                                        <div class="font-medium text-gray-900">
                                            {user.name}
                                        </div>
                                    </td>
                                    <td class="px-4 py-3 text-gray-700"
                                        >{user.email}</td
                                    >
                                    <td class="px-4 py-3">
                                        <span
                                            class={user.status === 0
                                                ? "text-gray-900"
                                                : "text-gray-500"}
                                        >
                                            {getStatusText(user.status)}
                                        </span>
                                    </td>
                                    <td class="px-4 py-3 text-gray-700">
                                        {formatTime(user.lastSeen)}
                                    </td>
                                </tr>
                            {/each}
                        {/if}
                    </tbody>
                </table>
            </div>
        </div>
    </div>
{/if}
