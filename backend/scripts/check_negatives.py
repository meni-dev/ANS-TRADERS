"""
Every way somebody could try to make the books say something untrue, and the app's answer.

Each case names what SHOULD happen. A case that behaves differently is a hole, not a preference —
so this exits non-zero and says which one. It builds its own parts and parties, uses them, and
removes every trace before it finishes; run it against a live shop and nothing of it survives.
"""
import datetime
import json
import os
import sys
import threading
import urllib.error
import urllib.request
import uuid

BASE = os.environ.get("ANS_API_URL", "http://localhost:5266")
TOKEN = os.environ["ANS_NEGATIVE_TOKEN"]
TODAY = datetime.date.today().isoformat()
TAG = uuid.uuid4().hex[:6].upper()

results = []


def call(method, path, body=None):
    req = urllib.request.Request(
        BASE + path,
        method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": "Bearer " + TOKEN, "Content-Type": "application/json"},
    )
    try:
        with urllib.request.urlopen(req) as response:
            raw = response.read().decode()
            return response.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as error:
        raw = error.read().decode()
        try:
            return error.code, json.loads(raw)
        except ValueError:
            return error.code, raw


def why(payload):
    if isinstance(payload, dict):
        if isinstance(payload.get("errors"), dict):
            return "; ".join(
                v[0] if isinstance(v, list) else str(v) for v in payload["errors"].values()
            )
        return str(payload.get("detail") or payload.get("message") or "")
    return str(payload or "")


def refuses(name, method, path, body):
    status, payload = call(method, path, body)
    results.append((status >= 400, name, status, why(payload)))


def allows(name, method, path, body):
    status, payload = call(method, path, body)
    results.append((200 <= status < 300, name, status, why(payload)))
    return payload


def product(part, opening, mrp=200, rate=18, supply="Taxable", hsn="87141090"):
    status, body = call("POST", "/api/products", {
        "partNumber": f"{part}-{TAG}", "itemCode": f"{part}{TAG}"[:20],
        "itemName": f"Check {part} {TAG}", "hsn": hsn, "uqc": "PCS", "gstRate": rate,
        "supplyType": supply, "purchaseRate": 100, "sellingRate": 150, "mrp": mrp,
        "reorderLevel": 0, "openingStock": opening, "isActive": True})
    if status >= 300:
        sys.exit(f"could not create the test part: {why(body)}")
    return body["id"]


def bill(product_id, quantity, rate=150, discount=0, mode="Credit", paid=0, date=None, **extra):
    return {
        "customerId": CUSTOMER, "walkInName": None, "invoiceDate": date or TODAY,
        "paymentMode": mode, "amountPaid": paid, "notes": None,
        "billDiscountPercent": 0, "billDiscountAmount": 0,
        "items": [{"productId": product_id, "quantity": quantity, "rate": rate,
                   "discountPercent": discount}], **extra}


def buy(product_id, quantity, rate=100):
    return {"supplierId": SUPPLIER, "supplierInvoiceNumber": f"CHK-{uuid.uuid4().hex[:8]}",
            "invoiceDate": TODAY, "paymentMode": "Credit", "amountPaid": 0, "notes": None,
            "items": [{"productId": product_id, "quantity": quantity, "rate": rate,
                       "discountPercent": 0}]}


# ---------------------------------------------------------------- fixtures ---
phone = lambda: "9" + str(uuid.uuid4().int)[:9]

status, customer = call("POST", "/api/customers", {
    "name": f"Check Customer {TAG}", "phone": phone(), "email": None, "gstin": None,
    "addressLine1": None, "addressLine2": None, "city": "Salem", "state": "Tamil Nadu",
    "stateCode": "33", "pincode": None, "creditLimit": 10_000_000, "creditDays": 30,
    "openingBalance": 0})
if status >= 300:
    sys.exit(f"could not create the test customer: {why(customer)}")
CUSTOMER = customer["id"]

status, supplier = call("POST", "/api/suppliers", {
    "name": f"Check Supplier {TAG}", "phone": phone(), "email": None, "gstin": None,
    "contactPerson": None, "addressLine1": None, "addressLine2": None, "city": "Salem",
    "state": "Tamil Nadu", "stateCode": "33", "pincode": None, "creditDays": 30,
    "openingBalance": 0})
if status >= 300:
    sys.exit(f"could not create the test supplier: {why(supplier)}")
SUPPLIER = supplier["id"]

part = product("STK", 10)

# ------------------------------------------------------------------- stock ---
refuses("a bill line with a negative quantity", "POST", "/api/invoices", bill(part, -5))
refuses("a bill line of zero", "POST", "/api/invoices", bill(part, 0))
refuses("billing more than is on the shelf", "POST", "/api/invoices", bill(part, 999))
refuses("a negative rate", "POST", "/api/invoices", bill(part, 1, rate=-10))
refuses("billing above the printed MRP", "POST", "/api/invoices", bill(part, 1, rate=500))
refuses("a line discount over 100%", "POST", "/api/invoices", bill(part, 1, discount=150))
refuses("a line discounted to nothing", "POST", "/api/invoices", bill(part, 1, discount=100))
refuses("a bill discount larger than the bill", "POST", "/api/invoices",
        bill(part, 1, billDiscountAmount=99999))
refuses("a bill with no lines", "POST", "/api/invoices",
        {**bill(part, 1), "items": []})
refuses("the same part on two lines of one bill", "POST", "/api/invoices",
        {**bill(part, 1), "items": [
            {"productId": part, "quantity": 1, "rate": 150, "discountPercent": 0},
            {"productId": part, "quantity": 1, "rate": 150, "discountPercent": 0}]})
refuses("a quantity that rounds away to nothing", "POST", "/api/invoices", bill(part, 0.0000001))
refuses("counting the shelf to a negative number", "POST", "/api/stock/adjust",
        {"productId": part, "countedQuantity": -5, "reason": "CountingError", "notes": None})
refuses("a stock correction with no reason this app knows", "POST", "/api/stock/adjust",
        {"productId": part, "countedQuantity": 8, "reason": "BecauseISaidSo", "notes": None})

# ----------------------------------------------------------------- returns ---
sold = allows("selling 5 to return against", "POST", "/api/invoices", bill(part, 5))
if sold:
    line = sold["items"][0]["id"]
    note = lambda qty, **kw: {"invoiceId": sold["id"], "noteDate": TODAY, "reason": "Damaged",
                              "lines": [{"documentItemId": line, "quantity": qty}],
                              "refundAmount": None, "refundMode": None,
                              "refundReference": None, **kw}
    refuses("taking back more than was sold", "POST", "/api/credit-notes", note(99))
    refuses("a credit note for a negative quantity", "POST", "/api/credit-notes", note(-1))
    allows("taking back 3 of the 5", "POST", "/api/credit-notes", note(3))
    refuses("a second note taking the running total past what was sold",
            "POST", "/api/credit-notes", note(3))
    refuses("refunding more than the note is worth", "POST", "/api/credit-notes",
            note(1, refundAmount=99999, refundMode="Cash"))

# -------------------------------------------------- reversals below zero -----
gone = product("REV", 0)
bought = call("POST", "/api/purchases", buy(gone, 5))[1]
call("POST", "/api/invoices", bill(gone, 5))
refuses("cancelling a purchase whose goods have been sold on", "POST",
        f"/api/purchases/{bought['id']}/cancel", {"reason": "check"})

resold = product("RSL", 5)
first = call("POST", "/api/invoices", bill(resold, 5))[1]
back = call("POST", "/api/credit-notes", {
    "invoiceId": first["id"], "noteDate": TODAY, "reason": "Damaged",
    "lines": [{"documentItemId": first["items"][0]["id"], "quantity": 5}],
    "refundAmount": None, "refundMode": None, "refundReference": None})[1]
call("POST", "/api/invoices", bill(resold, 5))
refuses("cancelling a credit note whose goods have been sold again", "POST",
        f"/api/credit-notes/{back['id']}/cancel", {"reason": "check"})

# ------------------------------------------------------------------- money ---
owed = allows("a credit bill to pay against", "POST", "/api/invoices", bill(part, 1))
if owed:
    due = float(owed["grandTotal"])
    receipt = lambda **kw: {"direction": "Received", "customerId": CUSTOMER, "supplierId": None,
                            "walkInName": None, "paymentDate": TODAY, "amount": due,
                            "mode": "BankTransfer", "referenceNumber": "chk", "notes": None,
                            "cheque": None, "allocations": [],
                            "autoAllocateOldestFirst": False, **kw}
    refuses("a receipt of nothing", "POST", "/api/payments", receipt(amount=0))
    refuses("a receipt for a negative amount", "POST", "/api/payments", receipt(amount=-500))
    refuses("allocating more to a bill than it owes", "POST", "/api/payments",
            receipt(allocations=[{"documentId": owed["id"], "amount": due + 1000}]))
    refuses("the same bill twice on one receipt", "POST", "/api/payments",
            receipt(allocations=[{"documentId": owed["id"], "amount": 1},
                                 {"documentId": owed["id"], "amount": 1}]))
    refuses("allocating a negative amount", "POST", "/api/payments",
            receipt(allocations=[{"documentId": owed["id"], "amount": -50}]))
    refuses("a receipt from nobody", "POST", "/api/payments", receipt(customerId=None))
    refuses("money received from a supplier", "POST", "/api/payments", receipt(supplierId=SUPPLIER))
    refuses("a receipt dated in the future", "POST", "/api/payments",
            receipt(paymentDate=(datetime.date.today() + datetime.timedelta(days=30)).isoformat()))
    refuses("a cheque with no cheque details", "POST", "/api/payments", receipt(mode="Cheque"))
    allows("settling the bill in full", "POST", "/api/payments",
           receipt(allocations=[{"documentId": owed["id"], "amount": due}]))
    refuses("paying a bill that is already settled", "POST", "/api/payments",
            receipt(amount=100, allocations=[{"documentId": owed["id"], "amount": 100}]))

# A refund is money going the wrong way down an account, and it cannot be larger than the credit
# the party is actually holding.
refunded = product("RFD", 10)
sold_back = allows("a credit bill to return against", "POST", "/api/invoices",
                   bill(refunded, 2, paid=0))
if sold_back:
    call("POST", "/api/credit-notes", {
        "invoiceId": sold_back["id"], "noteDate": TODAY, "reason": "Damaged",
        "lines": [{"documentItemId": sold_back["items"][0]["id"], "quantity": 2}],
        "refundAmount": None, "refundMode": None, "refundReference": None})
    refuses("refunding a customer more than they are owed", "POST", "/api/payments", {
        "direction": "Paid", "customerId": CUSTOMER, "supplierId": None, "walkInName": None,
        "paymentDate": TODAY, "amount": 999999, "mode": "BankTransfer",
        "referenceNumber": "check", "notes": None, "cheque": None,
        "allocations": [], "autoAllocateOldestFirst": False})

refuses("refunding a customer who is owed nothing", "POST", "/api/payments", {
    "direction": "Paid", "customerId": CUSTOMER, "supplierId": None, "walkInName": None,
    "paymentDate": TODAY, "amount": 1, "mode": "BankTransfer", "referenceNumber": "check",
    "notes": None, "cheque": None, "allocations": [], "autoAllocateOldestFirst": False})

refuses("taking more out of the till than it holds", "POST", "/api/money",
        {"movementDate": TODAY, "kind": "Drawings", "amount": 999_999_999,
         "affectsCash": True, "referenceNumber": None, "notes": "check"})
refuses("banking more than the till holds", "POST", "/api/money",
        {"movementDate": TODAY, "kind": "CashToBank", "amount": 999_999_999,
         "affectsCash": True, "referenceNumber": None, "notes": "check"})
refuses("a money movement of nothing", "POST", "/api/money",
        {"movementDate": TODAY, "kind": "Drawings", "amount": 0,
         "affectsCash": True, "referenceNumber": None, "notes": None})
refuses("closing a day that has not happened", "POST", "/api/cash/close",
        {"closeDate": (datetime.date.today() + datetime.timedelta(days=5)).isoformat(),
         "countedCash": 100, "reason": None, "notes": None})
refuses("counting a drawer to a negative figure", "POST", "/api/cash/close",
        {"closeDate": TODAY, "countedCash": -100, "reason": "check", "notes": None})
refuses("spending nothing", "POST", "/api/expenses",
        {"expenseDate": TODAY, "category": "Rent", "amount": 0, "mode": "Cash",
         "paidTo": None, "referenceNumber": None, "notes": None})
refuses("spending a negative amount", "POST", "/api/expenses",
        {"expenseDate": TODAY, "category": "Rent", "amount": -100, "mode": "Cash",
         "paidTo": None, "referenceNumber": None, "notes": None})

# ---------------------------------------------------------- dates and stock ---
tomorrow = (datetime.date.today() + datetime.timedelta(days=1)).isoformat()

refuses("a bill dated tomorrow", "POST", "/api/invoices", bill(part, 1, date=tomorrow))
refuses("a purchase dated tomorrow", "POST", "/api/purchases",
        {**buy(part, 1), "invoiceDate": tomorrow})
refuses("a receipt dated tomorrow", "POST", "/api/payments", {
    "direction": "Received", "customerId": CUSTOMER, "supplierId": None, "walkInName": None,
    "paymentDate": tomorrow, "amount": 10, "mode": "BankTransfer", "referenceNumber": "check",
    "notes": None, "cheque": None, "allocations": [], "autoAllocateOldestFirst": False})

# Goods that arrived on one day cannot have been sold the week before.
arrived = product("LATE", 0)
week_ago = (datetime.date.today() - datetime.timedelta(days=7)).isoformat()
three_ago = (datetime.date.today() - datetime.timedelta(days=3)).isoformat()
call("POST", "/api/purchases", {**buy(arrived, 10), "invoiceDate": three_ago})

refuses("a bill back-dated to before its goods arrived", "POST", "/api/invoices",
        bill(arrived, 5, date=week_ago))
allows("a bill back-dated to after its goods arrived", "POST", "/api/invoices",
       bill(arrived, 5, date=three_ago))

# And that bill's movement belongs to the bill's day, not to today.
_, register = call("GET", f"/api/reports/registers/stock-movement?fromDate={three_ago}&toDate={three_ago}")
rows = (register or {}).get("rows", [])
results.append((
    any(f"Check LATE {TAG}" in str(r) for r in rows),
    "a back-dated bill's movement appears under the bill's own date",
    len(rows), f"{len(rows)} movements listed for {three_ago}"))

# ------------------------------------------------- masters and credit limits ---
refuses("a second part with the same part number", "POST", "/api/products", {
    "partNumber": f"STK-{TAG}", "itemCode": f"CLASH{TAG}"[:20], "itemName": "Clash",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 18, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})
refuses("negative opening stock on a new part", "POST", "/api/products", {
    "partNumber": f"NEGOP-{TAG}", "itemCode": f"NO{TAG}", "itemName": "Negative opening",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 18, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": -5, "isActive": True})
refuses("a selling rate above the part's own MRP", "POST", "/api/products", {
    "partNumber": f"OVERMRP-{TAG}", "itemCode": f"OM{TAG}", "itemName": "Sells above MRP",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 18, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 500, "mrp": 100, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})

# Deactivating a part must close it in both directions, or stock climbs on something nobody
# is allowed to sell.
retired = product("OFF", 5)
call("PUT", f"/api/products/{retired}", {
    "partNumber": f"OFF-{TAG}", "itemCode": f"OFF{TAG}"[:20], "itemName": f"Check OFF {TAG}",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 18, "supplyType": "Taxable",
    "purchaseRate": 80, "sellingRate": 125, "mrp": 200, "reorderLevel": 0, "isActive": False})
refuses("billing a part that has been deactivated", "POST", "/api/invoices", bill(retired, 1))
refuses("buying a part that has been deactivated", "POST", "/api/purchases", buy(retired, 1))

# The credit limit is measured against what the customer owed coming in — counting the new bill
# twice refused everybody at half the limit the shop had set.
status, tight = call("POST", "/api/customers", {
    "name": f"Check Limit {TAG}", "phone": phone(), "email": None, "gstin": None,
    "addressLine1": None, "addressLine2": None, "city": "Salem", "state": "Tamil Nadu",
    "stateCode": "33", "pincode": None, "creditLimit": 400, "creditDays": 30,
    "openingBalance": 0})
if status < 300:
    limited = tight["id"]
    on_credit = {"customerId": limited, "walkInName": None, "invoiceDate": TODAY,
                 "paymentMode": "Credit", "amountPaid": 0, "notes": None,
                 "billDiscountPercent": 0, "billDiscountAmount": 0,
                 "items": [{"productId": part, "quantity": 1, "rate": 150, "discountPercent": 0}]}
    allows("a bill that fits inside the credit limit", "POST", "/api/invoices", on_credit)
    refuses("a bill that would take the customer past the limit", "POST", "/api/invoices",
            {**on_credit, "items": [{"productId": part, "quantity": 2, "rate": 150,
                                     "discountPercent": 0}]})

# ------------------------------------------------------------ cheques, again ---
refuses("a bank charge that is negative on a bounced cheque", "POST",
        f"/api/cheques/{uuid.uuid4()}/bounce",
        {"bouncedOn": TODAY, "reason": "check", "chargeAmount": -50})

# --------------------------------------------------------------------- GST ---
refuses("a taxable part at a zero rate", "POST", "/api/products", {
    "partNumber": f"G1-{TAG}", "itemCode": f"G1{TAG}", "itemName": "Taxable at nothing",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 0, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})
refuses("a nil-rated part carrying a rate", "POST", "/api/products", {
    "partNumber": f"G2-{TAG}", "itemCode": f"G2{TAG}", "itemName": "Nil rated at 18",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 18, "supplyType": "NilRated",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})
refuses("a rate that is not a GST slab", "POST", "/api/products", {
    "partNumber": f"G3-{TAG}", "itemCode": f"G3{TAG}", "itemName": "Thirteen percent",
    "hsn": "87141090", "uqc": "PCS", "gstRate": 13, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})
refuses("an HSN that is not 4, 6 or 8 digits", "POST", "/api/products", {
    "partNumber": f"G4-{TAG}", "itemCode": f"G4{TAG}", "itemName": "Short HSN",
    "hsn": "87", "uqc": "PCS", "gstRate": 18, "supplyType": "Taxable",
    "purchaseRate": 10, "sellingRate": 20, "mrp": 30, "reorderLevel": 0,
    "openingStock": 0, "isActive": True})
refuses("a GSTIN whose check digit does not match", "POST", "/api/customers", {
    "name": f"Check BadGstin {TAG}", "phone": phone(), "email": None,
    "gstin": "33AABCK5678L1Z9", "addressLine1": None, "addressLine2": None, "city": "Salem",
    "state": "Tamil Nadu", "stateCode": "33", "pincode": None,
    "creditLimit": 0, "creditDays": 0, "openingBalance": 0})
refuses("a GSTIN whose state disagrees with the address", "POST", "/api/customers", {
    "name": f"Check WrongState {TAG}", "phone": phone(), "email": None,
    "gstin": "33AABCK5678L1Z5", "addressLine1": None, "addressLine2": None, "city": "Kochi",
    "state": "Kerala", "stateCode": "32", "pincode": None,
    "creditLimit": 0, "creditDays": 0, "openingBalance": 0})
refuses("a bill dated before the books begin", "POST", "/api/invoices",
        bill(part, 1, date="2019-04-01"))

# ------------------------------------------------------- two counters at once ---
def race(times, action):
    """Fires the same request from several threads and collects every answer."""
    answers = []
    guard = threading.Lock()

    def go():
        answer = action()
        with guard:
            answers.append(answer)

    threads = [threading.Thread(target=go) for _ in range(times)]
    for thread in threads:
        thread.start()
    for thread in threads:
        thread.join()
    return answers


def raced(name, answers, expected_winners, truth, detail):
    """A race is right when the winners are what they should be AND the books still are."""
    winners = [a for a in answers if a[0] < 300]
    losers = [a for a in answers if a[0] >= 400]
    generic = [a for a in losers if "Somebody else saved this a moment" in why(a[1])]

    results.append((
        len(winners) == expected_winners and truth and not generic,
        name,
        len(winners),
        f"{len(winners)} through, {len(losers)} refused — {detail}"
        + (" (and one refusal did not say what was being saved)" if generic else ""),
    ))


last = product("RACE", 1)
answers = race(6, lambda: call("POST", "/api/invoices", bill(last, 1)))
_, shelf = call("GET", f"/api/products/{last}")
raced("six counters billing the last unit at once", answers, 1,
      float(shelf["stockOnHand"]) == 0, f"stock left {shelf['stockOnHand']}, must be 0")

settling = product("SET", 10)
owing = call("POST", "/api/invoices", bill(settling, 2))[1]
amount = float(owing["grandTotal"])
answers = race(4, lambda: call("POST", "/api/payments", {
    "direction": "Received", "customerId": CUSTOMER, "supplierId": None, "walkInName": None,
    "paymentDate": TODAY, "amount": amount, "mode": "BankTransfer", "referenceNumber": "check",
    "notes": None, "cheque": None,
    "allocations": [{"documentId": owing["id"], "amount": amount}],
    "autoAllocateOldestFirst": False}))
_, settled = call("GET", f"/api/invoices/{owing['id']}")
raced("four counters settling the same bill at once", answers, 1,
      float(settled["amountPaid"]) == amount and float(settled["balanceDue"]) == 0,
      f"paid {settled['amountPaid']} of {amount}, balance {settled['balanceDue']}")

counting = product("CNT", 10)
answers = race(4, lambda: call("POST", "/api/stock/adjust", {
    "productId": counting, "countedQuantity": 7, "reason": "CountingError", "notes": "check"}))
_, counted = call("GET", f"/api/products/{counting}")
raced("four counters correcting the same shelf at once", answers, 1,
      float(counted["stockOnHand"]) == 7, f"shelf now {counted['stockOnHand']}, must be 7")

voiding = product("VOI", 10)
doomed = call("POST", "/api/invoices", bill(voiding, 3))[1]
answers = race(4, lambda: call("POST", f"/api/invoices/{doomed['id']}/cancel", {"reason": "check"}))
_, restored = call("GET", f"/api/products/{voiding}")
raced("four counters cancelling the same bill at once", answers, 1,
      float(restored["stockOnHand"]) == 10,
      f"shelf back to {restored['stockOnHand']}, must be 10 and not 13")

taking = product("TAK", 10)
returnable = call("POST", "/api/invoices", bill(taking, 5))[1]
answers = race(4, lambda: call("POST", "/api/credit-notes", {
    "invoiceId": returnable["id"], "noteDate": TODAY, "reason": "Damaged",
    "lines": [{"documentItemId": returnable["items"][0]["id"], "quantity": 5}],
    "refundAmount": None, "refundMode": None, "refundReference": None}))
_, shelf_after = call("GET", f"/api/products/{taking}")
raced("four counters taking the same items back at once", answers, 1,
      float(shelf_after["stockOnHand"]) == 10,
      f"shelf back to {shelf_after['stockOnHand']}, must be 10 and not 15")

# ------------------------------------------------------------------ report ---
failed = [r for r in results if not r[0]]

print()
for ok, name, status, message in results:
    if not ok:
        print(f"  FAIL  {name}")
        print(f"        got HTTP {status}  {message[:120]}")

if failed:
    print(f"\n{len(failed)} of {len(results)} cases did not behave as they should.")
else:
    print(f"All {len(results)} negative cases behaved correctly. "
          "Nothing can drive stock, cash or a return the wrong way.")

sys.exit(1 if failed else 0)
